@echo off

set directory=%~dp0
Pushd %directory%

set "params=%*"
cd /d "%~dp0" && ( if exist "%temp%\getadmin.vbs" del "%temp%\getadmin.vbs" ) && fsutil dirty query %systemdrive% 1>nul 2>nul || (  echo Set UAC = CreateObject^("Shell.Application"^) : UAC.ShellExecute "cmd.exe", "/k cd ""%~sdp0"" && ""%~s0 %params%""", "", "runas", 1 >> "%temp%\getadmin.vbs" && "%temp%\getadmin.vbs" && exit /B )

(type MDRPSystemStartpt1.xml)>ManufacturedMDRPStartup.xml
(echo       ^<Command^>%cd%\RunHidden.vbs^</Command^> )>>ManufacturedMDRPStartup.xml
(type MDRPSystemStartpt2.xml)>>ManufacturedMDRPStartup.xml

schtasks /create /tn "MDRP Startup" /XML ManufacturedMDRPStartup.xml

pause 

exit