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

if not exist musicbeelocation.dat call FindMusicBee.bat

rem if not exist %loc% set /a "loc=%%loc:~0,%result%%%"

echo Setting security logger
auditpol /set /category:{6997984C-797A-11D9-BED3-505054503030} /success:enable>nul

(type LinkWithMusicBeept1.xml && type musicbeelocation.dat && type LinkWithGroovept4.xml)>LinkWithMusicBeeOpen.xml
echo %cd%\RunHidden.vbs>>LinkWithMusicBeeOpen.xml
type LinkWithGroovept2.xml>>LinkWithMusicBeeOpen.xml

echo Creating tasks
schtasks /create /tn MusicBeeRichPresenceOpen /XML LinkWithMusicBeeOpen.xml

set /p header_close=<LinkWithMusicBeept3.xml

(type LinkWithMusicBeept3.xml && type musicbeelocation.dat && type LinkWithGroovept4.xml)>LinkWithMusicBeeClose.xml
echo %cd%\KillHidden.vbs>>LinkWithMusicBeeClose.xml
type LinkWithGroovept2.xml>>LinkWithMusicBeeClose.xml

schtasks /create /tn MusicBeeRichPresenceClose /XML LinkWithMusicBeeClose.xml

pause

goto :eof

:fnf
echo Could not locate Music.UI.exe
pause
goto :eof