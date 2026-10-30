using System;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;
class Setup:Form {
 const string Name="Easy MP3 to Video";
 const string Reg=@"Software\Microsoft\Windows\CurrentVersion\Uninstall\AbeDesigns.EasyMP3ToVideo";
 static string Dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","Abe Designs","Easy MP3 to Video");
 static string Shortcut=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),Name+".lnk");
 Button install=new Button();
 [STAThread] static void Main(string[] args){Application.EnableVisualStyles();if(args.Length>0&&args[0]=="/uninstall"){string temp=Path.Combine(Path.GetTempPath(),"EasyMP3ToVideo-Uninstall-"+Guid.NewGuid().ToString("N")+".exe");File.Copy(Application.ExecutablePath,temp);Process.Start(temp,"/uninstall-run");return;}if(args.Length>0&&args[0]=="/uninstall-run"){Uninstall();return;}Application.Run(new Setup());}
 Setup(){Text=Name+" Setup";Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);ClientSize=new Size(530,250);StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;Font=new Font("Segoe UI",11);BackColor=Color.FromArgb(245,248,252);
 Controls.Add(new Label{Text=Name,Font=new Font("Segoe UI",21,FontStyle.Bold),AutoSize=true,Location=new Point(25,20)});
 Controls.Add(new Label{Text="Installs for your Windows account with Start Menu,\ndesktop shortcut and normal Windows uninstall support.",AutoSize=true,Location=new Point(28,105)});
 install.Text="INSTALL AND OPEN";install.SetBounds(28,177,470,48);install.BackColor=Color.Teal;install.ForeColor=Color.White;install.Click+=(s,e)=>Install();Controls.Add(install);}
 static bool Running(){foreach(var p in Process.GetProcessesByName("Easy-MP3-to-Video")){p.Dispose();return true;}return false;}
 void Install(){try{
 if(Running())throw new Exception("Close Easy MP3 to Video before installing. Do not close it during a conversion.");
 Directory.CreateDirectory(Dir);
 foreach(string n in new[]{"Easy-MP3-to-Video.exe","LICENSE.txt","THIRD-PARTY-NOTICES.txt"}){string res=n.EndsWith(".exe")?"encoder":n;using(var source=Assembly.GetExecutingAssembly().GetManifestResourceStream(res))using(var dest=File.Create(Path.Combine(Dir,n))){source.CopyTo(dest);}}
 string exe=Path.Combine(Dir,"Easy-MP3-to-Video.exe");string un=Path.Combine(Dir,"Uninstall.exe");File.Copy(Application.ExecutablePath,un,true);
 Directory.CreateDirectory(Path.GetDirectoryName(Shortcut));
 ShellIntegration.Shortcut(Shortcut,exe,Dir);
 ShellIntegration.Shortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),Name+".lnk"),exe,Dir);
 using(var key=Registry.CurrentUser.CreateSubKey(Reg)){key.SetValue("DisplayName",Name);key.SetValue("DisplayVersion","1.0.1");key.SetValue("Publisher","Abe Designs");key.SetValue("InstallLocation",Dir);key.SetValue("DisplayIcon",exe+",0");key.SetValue("UninstallString","\""+un+"\" /uninstall");key.SetValue("NoModify",1);key.SetValue("NoRepair",1);key.SetValue("EstimatedSize",100);}
 ShellIntegration.SHChangeNotify(0x08000000,0,IntPtr.Zero,IntPtr.Zero);
 Process.Start(exe);Close();
 }catch(Exception ex){MessageBox.Show(this,"Setup could not finish.\n\n"+ex.Message,"Setup problem");}}
 static void Uninstall(){if(MessageBox.Show("Uninstall Easy MP3 to Video? Your audio, images, videos and saved FFmpeg settings will be kept.",Name,MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
 try{if(Running())throw new Exception("Close Easy MP3 to Video before uninstalling.");
 string exe=Path.Combine(Dir,"Easy-MP3-to-Video.exe");
 foreach(string l in new[]{Shortcut,Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),Name+".lnk")})if(File.Exists(l))File.Delete(l);
 foreach(string n in new[]{"Easy-MP3-to-Video.exe","LICENSE.txt","THIRD-PARTY-NOTICES.txt"}){string p=Path.Combine(Dir,n);if(File.Exists(p))File.Delete(p);}
 string un=Path.Combine(Dir,"Uninstall.exe");if(File.Exists(un))File.Delete(un);if(Directory.Exists(Dir)&&Directory.GetFileSystemEntries(Dir).Length==0)Directory.Delete(Dir);
 Registry.CurrentUser.DeleteSubKeyTree(Reg,false);
 ShellIntegration.SHChangeNotify(0x08000000,0,IntPtr.Zero,IntPtr.Zero);
 MessageBox.Show("Easy MP3 to Video was uninstalled. Your files and settings have been kept.",Name);
 }catch(Exception ex){MessageBox.Show("Uninstall could not finish.\n\n"+ex.Message,Name);}}
}
