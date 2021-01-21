@echo off

set directory=%~dp0
Pushd %directory%

echo Checking admin rights...
net session>nul

if '%ERRORLEVEL%'=='0' (

echo Setting security logger
auditpol /set /category:{6997984C-797A-11D9-BED3-505054503030} /success:enable>nul

type LinkWithGroovept1.xml>LinkWithGrooveOpen.xml
echo %cd%\RunHidden.bat>>LinkWithGrooveOpen.xml
type LinkWithGroovept2.xml>>LinkWithGrooveOpen.xml

echo Creating tasks
schtasks /create /tn GrooveRichPresenceOpen /XML LinkWithGrooveOpen.xml

type LinkWithGroovept3.xml>LinkWithGrooveClose.xml
echo %cd%\KillHidden.bat>>LinkWithGrooveClose.xml
type LinkWithGroovept2.xml>>LinkWithGrooveClose.xml

schtasks /create /tn GrooveRichPresenceClose /XML LinkWithGrooveClose.xml

pause
) else (
cls
echo Admin rights not detected. Please run with admin perms
pause
EXIT /B
) 