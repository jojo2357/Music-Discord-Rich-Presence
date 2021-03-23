:::
:::  /$$      /$$                     /$$           /$$$$$$$  /$$$$$$$  /$$$$$$$ 
::: | $$$    /$$$                    |__/          | $$__  $$| $$__  $$| $$__  $$
::: | $$$$  /$$$$ /$$   /$$  /$$$$$$$ /$$  /$$$$$$$| $$  \ $$| $$  \ $$| $$  \ $$
::: | $$ $$/$$ $$| $$  | $$ /$$_____/| $$ /$$_____/| $$  | $$| $$$$$$$/| $$$$$$$/
::: | $$  $$$| $$| $$  | $$|  $$$$$$ | $$| $$      | $$  | $$| $$__  $$| $$____/ 
::: | $$\  $ | $$| $$  | $$ \____  $$| $$| $$      | $$  | $$| $$  \ $$| $$      
::: | $$ \/  | $$|  $$$$$$/ /$$$$$$$/| $$|  $$$$$$$| $$$$$$$/| $$  | $$| $$      
::: |__/     |__/ \______/ |_______/ |__/ \_______/|_______/ |__/  |__/|__/      
:::
::: [1] Launch DRP Hidden
::: [2] Launch DRP
::: [3] Kill Hidden DRP
::: [4] Link Rich Presence with Groove
::: [5] Link with MusicBee
::: [6] Locate Groove
::: [7] Locate MusicBee
::: [8] Unlink Rich Presence with Groove
::: [9] Unlink from MusicBee
:::

@echo off
set "params=%*"
cd /d "%~dp0" && ( if exist "%temp%\getadmin.vbs" del "%temp%\getadmin.vbs" ) && fsutil dirty query %systemdrive% 1>nul 2>nul || (  echo Set UAC = CreateObject^("Shell.Application"^) : UAC.ShellExecute "cmd.exe", "/k cd ""%~sdp0"" && %~s0 %params%", "", "runas", 1 >> "%temp%\getadmin.vbs" && "%temp%\getadmin.vbs" && exit /B )
color e
set mypath=%~dp0
set mypath=%mypath:~0,-1%
:MENU
cls
for /f "delims=: tokens=*" %%A in ('findstr /b ::: "%~f0"') do @echo(%%A
SET /P O=Pick an option: 
echo ------------------------------------------------------------------------------
IF %O%==1 GOTO LAUNCHDRPHIDDEN
IF %O%==2 GOTO LAUNCHDRP
IF %O%==3 GOTO KILLHIDDENDRP
IF %O%==4 GOTO LINKDRPGROOVE
IF %O%==5 GOTO LINKDRPMUSICBEE
IF %O%==6 GOTO LOCATEGROOVE
IF %O%==7 GOTO LOCATEMUSICBEE
IF %O%==8 GOTO UNLINKDRPGROOVE
IF %O%==9 GOTO UNLINKDRPMUSICBEE
GOTO MENU
:LAUNCHDRPHIDDEN
call "%mypath%\GroovyRP\bin\Release\RunHidden.vbs"
GOTO MENU
:LAUNCHDRP
call "%mypath%\GroovyRP\bin\Release\RunHidden.bat"
GOTO MENU
:KILLHIDDENDRP
taskkill /im GroovyRP.exe
GOTO MENU
:LINKDRPGROOVE
call "%mypath%\GroovyRP\bin\Release\LinkWithGroove.bat"
GOTO MENU
:LINKDRPMUSICBEE
call "%mypath%\GroovyRP\bin\Release\LinkWithMusicBee.bat"
GOTO MENU
:LOCATEGROOVE
call "%mypath%\GroovyRP\bin\Release\FindGroove.bat"
GOTO MENU
:LOCATEMUSICBEE
call "%mypath%\GroovyRP\bin\Release\FindMusicBee.bat"
GOTO MENU
:UNLINKDRPGROOVE
call "%mypath%\GroovyRP\bin\Release\UnlinkFromGroove.bat"
GOTO MENU
:UNLINKDRPMUSICBEE
call "%mypath%\GroovyRP\bin\Release\UnlinkFromMusicBee.bat"
GOTO MENU
