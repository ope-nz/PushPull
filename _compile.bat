@echo off
cd /d "D:\Dropbox\My Apps\DotNet\GFD"
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe ^
  /target:winexe /platform:x64 /out:PushPull.exe ^
  /win32icon:icon.ico ^
  /r:System.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll ^
  /r:System.Net.dll /r:System.Web.Extensions.dll /r:System.Security.dll ^
  /r:Microsoft.VisualBasic.dll ^
  Program.cs GitHub.cs ConfigManager.cs SyncEngine.cs AppIcon.cs ^
  SettingsDialog.cs ProjectDialog.cs ^
  MainForm.Designer.cs MainForm.cs
echo EXIT CODE: %ERRORLEVEL%

timeout /t 2 /nobreak >nul
.\Tools\SetExeFileInfo.exe -action setfileinfo -exe PushPull.exe -key "Copyright" -value "Ope Ltd"
timeout /t 2 /nobreak >nul
.\Tools\SetExeFileInfo.exe -action setfileinfo -exe PushPull.exe -key "ProductName" -value "PushPull for GitHub"
timeout /t 2 /nobreak >nul
.\Tools\SetExeFileInfo.exe -action setfileinfo -exe PushPull.exe -key "LegalCopyright" -value "https://github.com/ope-nz/PushPull"

timeout /t 2 /nobreak >nul
for /f "tokens=*" %%v in ('powershell -NoProfile -Command "Get-Date -Format 'yyyy.MM.dd.HHmm'"') do set BUILD_VERSION=%%v
.\Tools\SetExeFileInfo.exe -action setversion -exe PushPull.exe -version "%BUILD_VERSION%"
echo Version: %BUILD_VERSION%

pause
