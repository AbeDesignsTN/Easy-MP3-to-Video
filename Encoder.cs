using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace HcrEncoder {
static class Program {
 [STAThread] static void Main(string[] args) {
  ShellIntegration.SetCurrentProcessExplicitAppUserModelID(ShellIntegration.AppId);
  Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
  if(args.Length>0 && args[0]=="--self-test"){Environment.Exit(SelfTest.Run());return;}
  Application.Run(new EncoderForm());
 }
}
public static class Engine {
 public static string DefaultFolder=@"C:\Users\thehb\OneDrive\Documents\Heartbeat Christian Radio\Coffee & The Bible";
 public static string Quote(string s){return "\""+s.Replace("\"","")+"\"";}
 public static string Normalize(string s){return Regex.Replace(Path.GetFileNameWithoutExtension(s).ToLowerInvariant().Replace("youtube",""),"[^a-z0-9]","");}
 public static DateTime? EpisodeDate(string name){
  Match m=Regex.Match(name,@"(?<!\d)(20\d{2})[-_.]?(\d{2})[-_.]?(\d{2})(?!\d)");DateTime d;
  if(m.Success&&DateTime.TryParseExact(m.Groups[1].Value+m.Groups[2].Value+m.Groups[3].Value,"yyyyMMdd",CultureInfo.InvariantCulture,DateTimeStyles.None,out d))return d;
  m=Regex.Match(name,@"(?i)(JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC)[-_. ]?(\d{1,2})[-_. ]+(20\d{2})");
  if(m.Success&&DateTime.TryParseExact(m.Groups[1].Value+" "+m.Groups[2].Value+" "+m.Groups[3].Value,"MMM d yyyy",CultureInfo.InvariantCulture,DateTimeStyles.None,out d))return d;
  return null;
 }
 public static string FindImage(string audio){
  DateTime? date=EpisodeDate(Path.GetFileName(audio));
  var images=Directory.GetFiles(Path.GetDirectoryName(audio)).Where(p=>new[]{".jpg",".jpeg"}.Contains(Path.GetExtension(p).ToLowerInvariant())&&Path.GetFileName(p).IndexOf("YouTube",StringComparison.OrdinalIgnoreCase)>=0).ToArray();
  string exact=images.FirstOrDefault(p=>Normalize(p)==Normalize(audio));if(exact!=null)return exact;
  if(date.HasValue)return images.Where(p=>EpisodeDate(Path.GetFileName(p))==date).OrderByDescending(File.GetLastWriteTime).FirstOrDefault();
  return images.Where(p=>File.GetLastWriteTime(p).Date==File.GetLastWriteTime(audio).Date).OrderByDescending(File.GetLastWriteTime).FirstOrDefault();
 }
 public static string FindFfmpeg(){
  string saved=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"HCRVideoEncoder","ffmpeg-path.txt");
  if(File.Exists(saved)){string p=File.ReadAllText(saved).Trim();if(File.Exists(p))return p;}
  string beside=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"ffmpeg.exe");if(File.Exists(beside))return beside;
  foreach(string dir in (Environment.GetEnvironmentVariable("PATH")??"").Split(';')){try{string p=Path.Combine(dir.Trim('"'),"ffmpeg.exe");if(File.Exists(p))return p;}catch{}}
  foreach(string r in new[]{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"OneDrive","Documents"),@"C:\ffmpeg",@"C:\Tools"}){
   if(!Directory.Exists(r))continue;string direct=Path.Combine(r,"bin","ffmpeg.exe");if(File.Exists(direct))return direct;
   foreach(string dir in Directory.GetDirectories(r,"ffmpeg*")){string p=Path.Combine(dir,"bin","ffmpeg.exe");if(File.Exists(p))return p;}
  }return null;
 }
 public static string Arguments(string audio,string image,string temporary,double duration){
  return "-hide_banner -nostdin -y -loop 1 -framerate 2 -i "+Quote(image)+" -i "+Quote(audio)+" -map 0:v:0 -map 1:a:0 -vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2:out_range=tv,setsar=1\" -c:v libx264 -preset veryfast -tune stillimage -crf 23 -pix_fmt yuv420p -color_range tv -c:a aac -b:a 160k -t "+duration.ToString("0.######",CultureInfo.InvariantCulture)+" -shortest -movflags +faststart -progress pipe:1 -nostats "+Quote(temporary);
 }
}
public class FileBox:TextBox {
 string value=""; ToolTip tip=new ToolTip(); public string FilePath {get{return value;}set{this.value=value??"";Text=Path.GetFileName(this.value);tip.SetToolTip(this,this.value);}}
}
public class EncoderForm:Form {
 FileBox audio=new FileBox(),image=new FileBox(),output=new FileBox();
 Button create=new Button(),cancel=new Button(),folder=new Button(),another=new Button();
 Label status=new Label(),elapsed=new Label(),note=new Label();ProgressBar progress=new ProgressBar();
 System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();Stopwatch watch=new Stopwatch();
 Process running;readonly object gate=new object();CancellationTokenSource cancellation;bool busy,closeAfterCancel;long microseconds;double duration;string completed;
 Color navy=Color.FromArgb(12,29,53),teal=Color.FromArgb(0,121,130);
 public EncoderForm(){
  Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);
  Text="Easy MP3 to Video \u2014 v1.0";ClientSize=new Size(800,600);MinimumSize=new Size(816,639);StartPosition=FormStartPosition.CenterScreen;BackColor=Color.FromArgb(245,248,252);Font=new Font("Segoe UI",10);AutoScaleMode=AutoScaleMode.Dpi;
  var header=new Panel{BackColor=navy,Dock=DockStyle.Top,Height=132};Controls.Add(header);
  header.Controls.Add(new Label{Text="EASY MP3 TO VIDEO",ForeColor=Color.White,Font=new Font("Segoe UI",20,FontStyle.Bold),AutoSize=true,Location=new Point(28,20)});
  header.Controls.Add(new Label{Text="Developed by Abe Designs",ForeColor=Color.FromArgb(72,217,223),Font=new Font("Segoe UI",16),AutoSize=true,Location=new Point(30,59)});
  header.Controls.Add(new Label{Text="AdsWithAttitude.com",ForeColor=Color.White,AutoSize=true,Location=new Point(32,99)});
  var about=new Button{Text="About / Credits",ForeColor=Color.White,BackColor=navy,FlatStyle=FlatStyle.Flat,Size=new Size(135,30),Location=new Point(635,94),Anchor=AnchorStyles.Top|AnchorStyles.Right};
  about.Click+=(s,e)=>MessageBox.Show(this,"Easy MP3 to Video\nVersion 1.0\n\nFree and open-source software.\n\nDeveloped by Abe Designs\nAdsWithAttitude.com\n\nPowered by FFmpeg.\n\nThis application is licensed under the MIT License.\nFFmpeg is a separate project, licensed under LGPL 2.1 or later, or GPL depending on its build. FFmpeg is not bundled with this application.\n\nFFmpeg: https://ffmpeg.org\nLicensing: https://ffmpeg.org/legal.html\nSee LICENSE.txt and THIRD-PARTY-NOTICES.txt in the download package.","About Easy MP3 to Video",MessageBoxButtons.OK,MessageBoxIcon.Information);
  header.Controls.Add(about);
  AddRow("Audio (MP3)",audio,160,BrowseAudio);AddRow("YouTube image (JPEG) — Recommended: 1920 × 1080 pixels (16:9)",image,237,BrowseImage);AddRow("Output (MP4)",output,314,BrowseOutput);
  create.Text="CREATE VIDEO";create.SetBounds(230,397,340,55);create.BackColor=teal;create.ForeColor=Color.White;create.FlatStyle=FlatStyle.Flat;create.Font=new Font("Segoe UI",15,FontStyle.Bold);create.Anchor=AnchorStyles.Top;create.Click+=async(s,e)=>await Encode();Controls.Add(create);
  note.SetBounds(30,370,740,23);note.ForeColor=Color.FromArgb(76,89,109);note.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;Controls.Add(note);
  progress.SetBounds(30,470,740,19);progress.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;Controls.Add(progress);
  status.SetBounds(30,501,570,30);status.Font=new Font("Segoe UI",12,FontStyle.Bold);status.ForeColor=teal;Controls.Add(status);
  elapsed.SetBounds(30,538,255,25);Controls.Add(elapsed);
  cancel.Text="CANCEL";cancel.SetBounds(640,528,130,39);cancel.Visible=false;cancel.Click+=(s,e)=>Cancel();Controls.Add(cancel);
  folder.Text="OPEN FOLDER";folder.SetBounds(435,535,155,40);folder.Visible=false;folder.Click+=(s,e)=>{try{Process.Start("explorer.exe","/select,"+Engine.Quote(completed));}catch{MessageBox.Show(this,"Your video is saved at:\n"+completed);}};Controls.Add(folder);
  another.Text="CREATE ANOTHER";another.SetBounds(605,535,165,40);another.Visible=false;another.Click+=(s,e)=>{folder.Visible=another.Visible=false;status.Text="";progress.Value=0;elapsed.Text="";AutoSelect();};Controls.Add(another);
  timer.Interval=250;timer.Tick+=(s,e)=>{if(!busy)return;int pct=duration>0?(int)Math.Min(99,Interlocked.Read(ref microseconds)/1000000.0/duration*100):0;progress.Value=Math.Max(0,pct);status.Text="Encoding: "+pct+"%";elapsed.Text="Elapsed: "+watch.Elapsed.ToString(@"hh\:mm\:ss");};
  FormClosing+=(s,e)=>{if(busy){e.Cancel=true;if(MessageBox.Show(this,"Cancel encoding and close?","Encoding in progress",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes){Cancel();closeAfterCancel=true;}}};
  Shown+=(s,e)=>AutoSelect();
 }
 void AddRow(string title,TextBox box,int y,Action action){
  Controls.Add(new Label{Text=title,AutoSize=true,Location=new Point(30,y),ForeColor=navy});box.SetBounds(30,y+28,625,28);box.ReadOnly=true;box.BackColor=Color.White;box.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;Controls.Add(box);
  var b=new Button{Text="Browse",Tag="browse"};b.SetBounds(670,y+26,100,32);b.Anchor=AnchorStyles.Top|AnchorStyles.Right;b.Click+=(s,e)=>action();Controls.Add(b);
 }
 string InitialDirectory(FileBox box){try{if(File.Exists(box.FilePath))return Path.GetDirectoryName(box.FilePath);}catch{}return Directory.Exists(Engine.DefaultFolder)?Engine.DefaultFolder:Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);}
 void AutoSelect(){try{
  if(!Directory.Exists(Engine.DefaultFolder)){note.Text="Choose your MP3 and YouTube JPEG to get started.";return;}
  string latest=Directory.GetFiles(Engine.DefaultFolder,"*.mp3").OrderByDescending(p=>Engine.EpisodeDate(Path.GetFileName(p))??File.GetLastWriteTime(p).Date).ThenByDescending(File.GetLastWriteTime).FirstOrDefault();
  if(latest!=null)SetAudio(latest);else note.Text="No MP3 found. Use Browse to choose your episode.";
 }catch{note.Text="Use Browse to choose your files.";}}
 void SetAudio(string p){audio.FilePath=p;output.FilePath=Path.ChangeExtension(p,".mp4");image.FilePath=Engine.FindImage(p)??"";note.Text=image.FilePath.Length>0?"Suggested files — verify the episode and image before creating your video.":"No matching YouTube JPEG found. Choose the correct image with Browse.";}
 void BrowseAudio(){using(var d=new OpenFileDialog{Filter="MP3 audio|*.mp3",InitialDirectory=InitialDirectory(audio)})if(d.ShowDialog(this)==DialogResult.OK){try{SetAudio(d.FileName);}catch{audio.FilePath=d.FileName;image.FilePath="";output.FilePath=Path.ChangeExtension(d.FileName,".mp4");}}}
 void BrowseImage(){using(var d=new OpenFileDialog{Filter="JPEG image|*.jpg;*.jpeg",InitialDirectory=InitialDirectory(image)})if(d.ShowDialog(this)==DialogResult.OK)image.FilePath=d.FileName;}
 void BrowseOutput(){using(var d=new SaveFileDialog{Filter="MP4 video|*.mp4",DefaultExt="mp4",AddExtension=true,OverwritePrompt=false,FileName=Path.GetFileName(output.FilePath),InitialDirectory=InitialDirectory(audio)})if(d.ShowDialog(this)==DialogResult.OK)output.FilePath=d.FileName;}
 void SetBusy(bool value){busy=value;audio.Enabled=image.Enabled=output.Enabled=create.Enabled=!value;foreach(Control c in Controls)if((string)c.Tag=="browse")c.Enabled=!value;cancel.Visible=value;folder.Visible=another.Visible=false;}
 void Cancel(){if(cancellation!=null)cancellation.Cancel();lock(gate){if(running!=null){try{running.Kill();}catch{}}}cancel.Enabled=false;status.Text="Cancelling…";}
 string Run(string exe,string args,CancellationToken token,bool report){
  token.ThrowIfCancellationRequested();var errors=new StringBuilder();var stdout=new StringBuilder();
  using(var p=new Process{StartInfo=new ProcessStartInfo(exe,args){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true}}){
   p.ErrorDataReceived+=(s,e)=>{if(e.Data!=null)lock(errors){if(errors.Length>24000)errors.Remove(0,12000);errors.AppendLine(e.Data);}};
   p.OutputDataReceived+=(s,e)=>{if(e.Data==null)return;if(report){if(e.Data.StartsWith("out_time_us=")){long v;if(long.TryParse(e.Data.Substring(12),out v))Interlocked.Exchange(ref microseconds,v);}}else stdout.AppendLine(e.Data);};
   lock(gate){token.ThrowIfCancellationRequested();running=p;p.Start();}p.BeginErrorReadLine();p.BeginOutputReadLine();
   try{while(!p.WaitForExit(200)){if(token.IsCancellationRequested){try{p.Kill();}catch{}}}p.WaitForExit();token.ThrowIfCancellationRequested();if(p.ExitCode!=0)throw new Exception(errors.ToString());return stdout.ToString();}finally{lock(gate){running=null;}}
  }
 }
 async Task Encode(){
  if(!File.Exists(audio.FilePath)||!File.Exists(image.FilePath)){MessageBox.Show(this,"Choose an MP3 and a JPEG image first.","Choose files");return;}
  if(Path.GetExtension(audio.FilePath).ToLowerInvariant()!=".mp3"||!new[]{".jpg",".jpeg"}.Contains(Path.GetExtension(image.FilePath).ToLowerInvariant())){MessageBox.Show(this,"Please select MP3 audio and a JPG or JPEG image.");return;}
  if(string.IsNullOrWhiteSpace(output.FilePath)||Path.GetExtension(output.FilePath).ToLowerInvariant()!=".mp4"){MessageBox.Show(this,"Choose an MP4 output filename.");return;}
  bool replace=File.Exists(output.FilePath);if(replace&&MessageBox.Show(this,"Replace this existing video?\n\n"+output.FilePath,"Confirm replacement",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
  string engine=Engine.FindFfmpeg();if(engine==null){MessageBox.Show(this,"Please locate your existing ffmpeg.exe. The app will remember it.","Locate FFmpeg");using(var d=new OpenFileDialog{Filter="FFmpeg|ffmpeg.exe"}){if(d.ShowDialog(this)!=DialogResult.OK)return;engine=d.FileName;}}
  string probe=Path.Combine(Path.GetDirectoryName(engine),"ffprobe.exe");if(!File.Exists(probe)){MessageBox.Show(this,"ffprobe.exe must be beside ffmpeg.exe. Choose the bin folder from your complete FFmpeg installation.","FFmpeg installation incomplete");return;}
  try{string config=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"HCRVideoEncoder");Directory.CreateDirectory(config);File.WriteAllText(Path.Combine(config,"ffmpeg-path.txt"),engine);}catch{}
  string a=audio.FilePath,i=image.FilePath,o=output.FilePath,temp=Path.Combine(Path.GetDirectoryName(o),".hcr-"+Guid.NewGuid().ToString("N")+".mp4");
  SetBusy(true);cancel.Enabled=true;progress.Value=0;Interlocked.Exchange(ref microseconds,0);duration=0;watch.Restart();timer.Start();cancellation=new CancellationTokenSource();var token=cancellation.Token;
  try{
   await Task.Run(()=>{
    string result=Run(probe,"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 "+Engine.Quote(a),token,false);double d;
    if(!double.TryParse(result.Trim(),NumberStyles.Float,CultureInfo.InvariantCulture,out d)||d<=0)throw new Exception("Cannot read audio duration");duration=d;
    Run(engine,Engine.Arguments(a,i,temp,d),token,true);token.ThrowIfCancellationRequested();
    if(File.Exists(o)){if(!replace)throw new IOException("An output file appeared while encoding.");File.Replace(temp,o,null);}else File.Move(temp,o);
   });
   completed=o;progress.Value=100;status.Text="✓ VIDEO COMPLETE";elapsed.Text="Elapsed: "+watch.Elapsed.ToString(@"hh\:mm\:ss");SetBusy(false);folder.Visible=another.Visible=true;note.Text=Path.GetFileName(o);
  }catch(OperationCanceledException){SetBusy(false);status.Text="Encoding cancelled";progress.Value=0;}
  catch(Exception ex){SetBusy(false);status.Text="Video could not be created";MessageBox.Show(this,Friendly(ex.Message),"Encoding problem",MessageBoxButtons.OK,MessageBoxIcon.Warning);try{string log=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"HCRVideoEncoder");Directory.CreateDirectory(log);File.WriteAllText(Path.Combine(log,"last-error.txt"),ex.ToString());}catch{}}
  finally{timer.Stop();watch.Stop();cancellation.Dispose();cancellation=null;try{if(File.Exists(temp))File.Delete(temp);}catch{}if(closeAfterCancel)Close();}
 }
 static string Friendly(string raw){string s=raw.ToLowerInvariant();if(s.Contains("space left")||s.Contains("disk full"))return "There is not enough free space for the video. Free some disk space and try again.";if(s.Contains("denied")||s.Contains("permission")||s.Contains("being used"))return "Windows could not save the video. Close any app using it, or choose another output folder.";if(s.Contains("invalid data")||s.Contains("duration")||s.Contains("decode"))return "The audio or image could not be read. Check that both files open normally and are downloaded from OneDrive.";if(s.Contains("appeared"))return "Another file was saved to this output name while encoding. Choose another output filename.";return "The video could not be created. Check that your files are available, the output folder is writable, and FFmpeg is complete. Technical details were saved for troubleshooting.";}
}
static class SelfTest {
 public static int Run(){try{
  if(Engine.EpisodeDate("20261025-2359-Todd.mp3")!=new DateTime(2026,10,25))throw new Exception("Audio date");
  if(Engine.EpisodeDate("Promo-OCT-25.2026-Pray-Without-Ceasing-YouTube.jpg")!=new DateTime(2026,10,25))throw new Exception("Image date");
  if(Engine.Normalize("Matthew Chapter 10.mp3")!=Engine.Normalize("Matthew Chapter 10 YouTube.jpg"))throw new Exception("Title matching");
  string a=Engine.Arguments("a.mp3","i.jpg","o.mp4",10.25);if(!a.Contains("-loop 1 -framerate 2")||!a.Contains("-shortest")||!a.Contains("-t 10.25"))throw new Exception("Encoding arguments");
  File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"self-test.txt"),"PASS: audio date, image date, title matching, encoding arguments\r\nFFmpeg: "+Engine.FindFfmpeg()+"\r\nMatched image: "+Engine.FindImage(Path.Combine(Engine.DefaultFolder,"20261025-2359-Todd.mp3")));return 0;
 }catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"self-test.txt"),ex.ToString());return 1;}}
}
}
