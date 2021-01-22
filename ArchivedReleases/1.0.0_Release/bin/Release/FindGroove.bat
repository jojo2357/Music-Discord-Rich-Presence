@echo off

set directory=%~dp0
Pushd %directory%

echo I need to find the location of groove music on your computer, and the easiest way to do that is to open it and see where it came from

echo Checking admin rights...
net session 1>nul 2>nul

if not '%ERRORLEVEL%'=='0' (
echo Admin rights not detected. Please run with admin perms
pause
EXIT /B
)

auditpol /set /category:{6997984C-797A-11D9-BED3-505054503030} /success:enable>nul

taskkill /im Music.UI.exe 1>nul 2>nul

echo Please ensure Groove is closed
timeout 10

cmd /c start mswindowsmusic:

echo waiting for groove to come to life
timeout 1

wevtutil qe Security /c:20 /f:text /rd:true /q:"*[System[(EventID=4688)]]">openedfiles.dat

findstr /i "Music.UI.exe" openedfiles.dat>filelocation.dat

set /p loc=<filelocation.dat

echo Groove is at %loc:~19%

set /a "result=%result%-19"

call set parsed=%loc:~19%

rem debug echo %parsed%

set /p fgiddle=<fdoogle.dat

>filelocation.dat (
echo|set /p="%parsed%%fgiddle%"
)

taskkill /im Music.UI.exe 1>nul 2>nul

del openedfiles.dat 1>nul 2>nul

if "%parsed%"=="~19" (
echo TASK FAILED. please run me again and make sure that groove is closed when you start me.
) else (
echo task completed. Please close groove idk y it didnt close on its own
)

pause