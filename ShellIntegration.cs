using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
public static class ShellIntegration {
 public const string AppId="AbeDesigns.EasyMP3ToVideo";
 [DllImport("shell32.dll",CharSet=CharSet.Unicode)] public static extern int SetCurrentProcessExplicitAppUserModelID(string id);
 [DllImport("ole32.dll")] static extern int PropVariantClear(ref PV p);
 [DllImport("shell32.dll")] public static extern void SHChangeNotify(uint e,uint f,IntPtr a,IntPtr b);
 [StructLayout(LayoutKind.Sequential)] public struct Key {public Guid fmt;public uint pid;public Key(uint p){fmt=new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");pid=p;}}
 [StructLayout(LayoutKind.Explicit,Size=24)] public struct PV {[FieldOffset(0)]public ushort vt;[FieldOffset(8)]public IntPtr value;}
 [ComImport,Guid("00021401-0000-0000-C000-000000000046")] class Link {}
 [ComImport,Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface Store {void GetCount(out uint n);void GetAt(uint i,out Key k);void GetValue(ref Key k,out PV p);void SetValue(ref Key k,ref PV p);void Commit();}
 [ComImport,Guid("000214F9-0000-0000-C000-000000000046"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface SL {
 void GetPath(StringBuilder p,int n,IntPtr data,uint flags);void GetIDList(out IntPtr p);void SetIDList(IntPtr p);void GetDescription(StringBuilder p,int n);void SetDescription([MarshalAs(UnmanagedType.LPWStr)]string p);void GetWorkingDirectory(StringBuilder p,int n);void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)]string p);void GetArguments(StringBuilder p,int n);void SetArguments([MarshalAs(UnmanagedType.LPWStr)]string p);void GetHotkey(out short h);void SetHotkey(short h);void GetShowCmd(out int c);void SetShowCmd(int c);void GetIconLocation(StringBuilder p,int n,out int i);void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)]string p,int i);void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)]string p,uint r);void Resolve(IntPtr w,uint f);void SetPath([MarshalAs(UnmanagedType.LPWStr)]string p);
 }
 public static void Shortcut(string file,string exe,string dir){object o=new Link();try{SL l=(SL)o;l.SetPath(exe);l.SetWorkingDirectory(dir);l.SetDescription("Easy MP3 to Video — Developed by Abe Designs");l.SetIconLocation(exe,0);l.SetShowCmd(1);Store s=(Store)o;Key k=new Key(5);PV p=new PV{vt=31,value=Marshal.StringToCoTaskMemUni(AppId)};try{s.SetValue(ref k,ref p);s.Commit();}finally{PropVariantClear(ref p);}((IPersistFile)o).Save(file,true);}finally{Marshal.FinalReleaseComObject(o);}}
}
