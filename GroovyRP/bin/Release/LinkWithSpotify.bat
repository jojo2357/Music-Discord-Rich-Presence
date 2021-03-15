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

if not exist spotifylocation.dat call FindSpotify.bat

rem if not exist %loc% set /a "loc=%%loc:~0,%result%%%"

echo Setting security logger
auditpol /set /category:{6997984C-797A-11D9-BED3-505054503030} /success:enable>nul

(type LinkWithSpotifypt1.xml && type spotifylocation.dat && type LinkWithGroovept4.xml)>LinkWithSpotifyOpen.xml
echo %cd%\RunHidden.vbs>>LinkWithSpotifyOpen.xml
type LinkWithGroovept2.xml>>LinkWithSpotifyOpen.xml

echo Creating tasks
schtasks /create /tn SpotifyRichPresenceOpen /XML LinkWithSpotifyOpen.xml

set /p header_close=<LinkWithSpotifypt3.xml

(type LinkWithSpotifypt3.xml && type spotifylocation.dat && type LinkWithGroovept4.xml)>LinkWithSpotifyClose.xml
echo %cd%\KillHidden.vbs>>LinkWithSpotifyClose.xml
type LinkWithGroovept2.xml>>LinkWithSpotifyClose.xml

schtasks /create /tn SpotifyRichPresenceClose /XML LinkWithSpotifyClose.xml

pause

goto :eof

:fnf
echo Could not locate Spotify.exe
pause
goto :eof