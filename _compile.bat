@echo off
cd /d "D:\Dropbox\My Apps\DotNet\GFD"
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe ^
  /target:winexe /platform:x64 /out:PushPull.exe ^
  /win32icon:icon.ico ^
  /r:System.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll ^
  /r:System.Net.dll /r:System.Web.Extensions.dll /r:System.Security.dll ^
  Program.cs GitHub.cs ConfigManager.cs SyncEngine.cs AppIcon.cs ^
  SettingsDialog.cs ProjectDialog.cs ^
  MainForm.Designer.cs MainForm.cs
echo EXIT CODE: %ERRORLEVEL%

.\Tools\SetExeFileInfo.exe -action setfileinfo -exe PushPull.exe -key "Copyright" -value "Ope Ltd"
.\Tools\SetExeFileInfo.exe -action setfileinfo -exe PushPull.exe -key "ProductName" -value "PushPull for GitHub"
.\Tools\SetExeFileInfo.exe -action setfileinfo -exe PushPull.exe -key "LegalCopyright" -value "https://github.com/ope-nz/PushPull"

.\Tools\SetExeFileInfo.exe -action setversion -exe PushPull.exe -version "1.0.0.0"

pause
