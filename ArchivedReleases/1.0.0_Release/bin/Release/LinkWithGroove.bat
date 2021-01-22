@echo off
rem setlocal ENABLEDELAYEDEXPANSION

set directory=%~dp0
Pushd %directory%

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
echo %cd%\RunHidden.bat>>LinkWithGrooveOpen.xml
type LinkWithGroovept2.xml>>LinkWithGrooveOpen.xml

echo Creating tasks
schtasks /create /tn GrooveRichPresenceOpen /XML LinkWithGrooveOpen.xml

set /p header_close=<LinkWithGroovept3.xml

(type LinkWithGroovept3.xml && type filelocation.dat && type LinkWithGroovept4.xml)>LinkWithGrooveClose.xml
echo %cd%\KillHidden.bat>>LinkWithGrooveClose.xml
type LinkWithGroovept2.xml>>LinkWithGrooveClose.xml

schtasks /create /tn GrooveRichPresenceClose /XML LinkWithGrooveClose.xml

pause

goto :eof

:fnf
echo Could not locate Music.UI.exe
pause
goto :eof