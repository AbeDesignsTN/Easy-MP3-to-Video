$ErrorActionPreference='Stop'
Set-Location -LiteralPath $PSScriptRoot
$c=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $c /nologo /codepage:65001 /target:winexe /platform:x64 /optimize+ /win32icon:App.ico /out:Easy-MP3-to-Video.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll Encoder.cs ShellIntegration.cs AssemblyInfo.cs
if($LASTEXITCODE){throw 'Encoder build failed'}
& $c /nologo /codepage:65001 /target:winexe /platform:x64 /optimize+ /win32icon:App.ico /out:Easy-MP3-to-Video-Setup.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /resource:Easy-MP3-to-Video.exe,encoder /resource:LICENSE.txt,LICENSE.txt /resource:THIRD-PARTY-NOTICES.txt,THIRD-PARTY-NOTICES.txt Setup.cs ShellIntegration.cs AssemblyInfo.cs
if($LASTEXITCODE){throw 'Setup build failed'}
