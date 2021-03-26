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
::: [0] Generate Shortcuts
::: [1] Launch DRP Hidden
::: [2] Launch DRP
::: [3] Kill Hidden DRP
::: [4] Link Rich Presence with Groove
::: [5] Link with MusicBee
::: [6] Unlink Rich Presence with Groove
::: [7] Unlink from MusicBee
:::

@echo off
color e
set mypath=%~dp0
set mypath=%mypath:~0,-1%
:MENU
cls
for /f "delims=: tokens=*" %%A in ('findstr /b ::: "%~f0"') do @echo(%%A
SET /P O=Pick an option: 
echo ------------------------------------------------------------------------------
IF %O%==0 GOTO LAUNCHDRPHIDDEN
IF %O%==1 GOTO LAUNCHDRPHIDDEN
IF %O%==2 GOTO LAUNCHDRP
IF %O%==3 GOTO KILLHIDDENDRP
IF %O%==4 GOTO LINKDRPGROOVE
IF %O%==5 GOTO LINKDRPMUSICBEE
IF %O%==6 GOTO UNLINKDRPGROOVE
IF %O%==7 GOTO UNLINKDRPMUSICBEE
GOTO MENU
:LAUNCHDRPHIDDEN
"%mypath%\GroovyRP\bin\Release\GroovyRP.exe" Shortcuts_Only
start "" "%mypath%\Shortcuts\Run MDRP Background.lnk"
exit
GOTO MENU
:LAUNCHDRP
call "%mypath%\GroovyRP\bin\Release\RunHidden.bat"
GOTO MENU
:KILLHIDDENDRP
call "%mypath%\KillHidden.bat"
GOTO MENU
:LINKDRPGROOVE
call "%mypath%\GroovyRP\bin\Release\LinkWithGroove.bat"
GOTO MENU
:LINKDRPMUSICBEE
call "%mypath%\GroovyRP\bin\Release\LinkWithMusicBee.bat"
GOTO MENU
:UNLINKDRPGROOVE
call "%mypath%\GroovyRP\bin\Release\UnlinkFromGroove.bat"
GOTO MENU
:UNLINKDRPMUSICBEE
call "%mypath%\GroovyRP\bin\Release\UnlinkFromMusicBee.bat"
GOTO MENU
