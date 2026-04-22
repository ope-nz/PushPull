@echo off
cd /d "D:\Dropbox\My Apps\DotNet\GFD"
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe ^
  /target:winexe /platform:x64 /out:GFD.exe ^
  /win32icon:icon.ico ^
  /r:System.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll ^
  /r:System.Net.dll /r:System.Web.Extensions.dll /r:System.Security.dll ^
  Program.cs GitHub.cs ConfigManager.cs SyncEngine.cs AppIcon.cs ^
  SettingsDialog.cs ProjectDialog.cs ^
  MainForm.Designer.cs MainForm.cs
echo EXIT CODE: %ERRORLEVEL%
pause
