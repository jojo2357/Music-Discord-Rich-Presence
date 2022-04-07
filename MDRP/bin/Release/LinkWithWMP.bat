@echo off
rem setlocal ENABLEDELAYEDEXPANSION

set directory=%~dp0
Pushd %directory%

set "params=%*"
cd /d "%~dp0" && ( if exist "%temp%\getadmin.vbs" del "%temp%\getadmin.vbs" ) && fsutil dirty query %systemdrive% 1>nul 2>nul || (  echo Set UAC = CreateObject^("Shell.Application"^) : UAC.ShellExecute "cmd.exe", "/k cd ""%~sdp0"" && ""%~s0 %params%""", "", "runas", 1 >> "%temp%\getadmin.vbs" && "%temp%\getadmin.vbs" && exit /B )

echo Checking admin rights...
net session>nul

cls

if not '%ERRORLEVEL%'=='0' (
echo Admin rights not detected. Please run with admin perms
pause
EXIT /B
)

if not exist wmplocation.dat call FindWMP.bat

rem if not exist %loc% set /a "loc=%%loc:~0,%result%%%"

echo Setting security logger
auditpol /set /category:{6997984C-797A-11D9-BED3-505054503030} /success:enable>nul

(type LinkWithWMPpt1.xml && type wmplocation.dat && type LinkWithGroovept4.xml)>LinkWithWMPOpen.xml
echo %cd%\RunHidden.vbs>>LinkWithWMPOpen.xml
type LinkWithGroovept2.xml>>LinkWithWMPOpen.xml

echo Creating tasks
schtasks /create /tn WMPRichPresenceOpen /XML LinkWithWMPOpen.xml

set /p header_close=<LinkWithWMPpt3.xml

(type LinkWithWMPpt3.xml && type wmplocation.dat && type LinkWithGroovept4.xml)>LinkWithWMPClose.xml
echo %cd%\KillHidden.vbs>>LinkWithWMPClose.xml
type LinkWithGroovept2.xml>>LinkWithWMPClose.xml

schtasks /create /tn WMPRichPresenceClose /XML LinkWithWMPClose.xml

pause

exit

:fnf
echo Could not locate microsoft.media.player.exe
pause
exit
goto :eof