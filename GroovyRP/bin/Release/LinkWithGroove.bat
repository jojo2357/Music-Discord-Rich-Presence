@echo off

set directory=%~dp0
Pushd %directory%

echo Checking admin rights...
net session>nul

if '%ERRORLEVEL%'=='0' (

echo Setting security logger
auditpol /set /category:{6997984C-797A-11D9-BED3-505054503030} /success:enable>nul

type LinkWithGroovept1.xml>LinkWithGroove.xml
echo %cd%\RunHidden.bat>>LinkWithGroove.xml
type LinkWithGroovept2.xml>>LinkWithGroove.xml

echo Creating task
schtasks /create /tn GrooveRichPresence /XML LinkWithGroove.xml

pause
) else (
cls
echo Admin rights not detected. Please run with admin perms
pause
EXIT /B
) 