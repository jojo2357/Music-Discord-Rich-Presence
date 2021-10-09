@echo off

set directory=%~dp0
Pushd %directory%

echo I need to find the location of MusicBee on your computer, and the easiest way to do that is to open it and see where it came from

echo Checking admin rights...
net session 1>nul 2>nul

if not '%ERRORLEVEL%'=='0' (
echo Admin rights not detected. Please run with admin perms
pause
EXIT /B
)

auditpol /set /category:{6997984C-797A-11D9-BED3-505054503030} /success:enable>nul

taskkill /im MusicBee.exe 1>nul 2>nul

echo Please ensure MusicBee is closed then reopen it. once you have, reopen music bee and then come back here and press any key to continue
pause>nul

wevtutil qe Security /c:200 /f:text /rd:true /q:"*[System[(EventID=4688)]]">openedfiles.dat

findstr /i "MusicBee.exe" openedfiles.dat>musicbeelocation.dat

set /p loc=<musicbeelocation.dat

echo MusicBee is at %loc:~19%

set /a "result=%result%-19"

call set parsed=%loc:~19%

rem debug echo %parsed%

set /p fgiddle=<fdoogle.dat

>musicbeelocation.dat (
echo|set /p="%parsed%%fgiddle%"
)

taskkill /im MusicBee.exe 1>nul 2>nul

del openedfiles.dat 1>nul 2>nul

if "%parsed%"=="~19" (
echo TASK FAILED. please run me again and make sure that MusicBee is closed when you start me.
) else (
echo task completed. Please close MusicBee idk y it didnt close on its own
)

pause