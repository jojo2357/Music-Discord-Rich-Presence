@echo off
rem setlocal ENABLEDELAYEDEXPANSION

set directory=%~dp0
Pushd %directory%

set "params=%*"
cd /d "%~dp0" && ( if exist "%temp%\getadmin.vbs" del "%temp%\getadmin.vbs" ) && fsutil dirty query %systemdrive% 1>nul 2>nul || (  echo Set UAC = CreateObject^("Shell.Application"^) : UAC.ShellExecute "cmd.exe", "/k cd ""%~sdp0"" && %~s0 %params%", "", "runas", 1 >> "%temp%\getadmin.vbs" && "%temp%\getadmin.vbs" && exit /B )

echo Checking admin rights...
net session>nul

cls

if not '%ERRORLEVEL%'=='0' (
echo Admin rights not detected. Please run with admin perms
pause
EXIT /B
)

if not exist filelocation.dat call FindGroove.bat

rem if not exist %loc% set /a "loc=%%loc:~0,%result%%%"

echo Setting security logger
auditpol /set /category:{6997984C-797A-11D9-BED3-505054503030} /success:enable>nul

(type LinkWithGroovept1.xml && type filelocation.dat && type LinkWithGroovept4.xml)>LinkWithGrooveOpen.xml
echo %cd%\RunHidden.vbs>>LinkWithGrooveOpen.xml
type LinkWithGroovept2.xml>>LinkWithGrooveOpen.xml

echo Creating tasks
schtasks /create /tn GrooveRichPresenceOpen /XML LinkWithGrooveOpen.xml

set /p header_close=<LinkWithGroovept3.xml

(type LinkWithGroovept3.xml && type filelocation.dat && type LinkWithGroovept4.xml)>LinkWithGrooveClose.xml
echo %cd%\KillHidden.vbs>>LinkWithGrooveClose.xml
type LinkWithGroovept2.xml>>LinkWithGrooveClose.xml

schtasks /create /tn GrooveRichPresenceClose /XML LinkWithGrooveClose.xml

pause

exit

goto :eof

:fnf
echo Could not locate Music.UI.exe
pause
exit
goto :eof