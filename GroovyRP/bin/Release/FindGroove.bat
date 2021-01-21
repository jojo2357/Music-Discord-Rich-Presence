@echo off

echo I need to find the location of groove music on your computer, and the easiest way to do that is to open it and see where it came from

pause

auditpol /set /category:{6997984C-797A-11D9-BED3-505054503030} /success:enable>nul

taskkill /im Music.UI.exe>nul

echo waiting for groove to close
timeout 2

cmd /c start mswindowsmusic:

echo waiting for groove to come to life
timeout 1

wevtutil qe Security /c:10 /f:text /rd:true /q:"*[System[(EventID=4688)]]">openedfiles.dat

findstr /i "Music.UI.exe" openedfiles.dat>filelocation.dat

set /p loc=<filelocation.dat

echo It is at %loc:~19%

set /a "result=%result%-19"

call set parsed=%loc:~19%

rem debug echo %parsed%

set /p fgiddle=<fdoogle.dat

>filelocation.dat (
echo|set /p="%parsed%%fgiddle%"
)

taskkill /im Music.UI.exe>nul

echo task completed. Please close groove idk y it didnt close on its own

pause

goto :eof