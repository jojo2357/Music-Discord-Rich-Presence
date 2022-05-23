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
::: [5] Link Rich Presence with Windows Media Player (Windows 11)
::: [6] Link Rich Presence with System Start
::: [7] Unlink Rich Presence with Groove
::: [8] Unlink Rich Presence with Windows Media Player (Windows 11)
::: [9] Unlink Rich Presence with System Start
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
IF %O%==5 GOTO LINKDRPWMP
IF %O%==6 GOTO LINKDRPSYSSTART
IF %O%==7 GOTO UNLINKDRPGROOVE
IF %O%==8 GOTO UNLINKDRPWMP
IF %O%==9 GOTO UNLINKDRPSYSSTART
GOTO MENU
:LAUNCHDRPHIDDEN
START "Music Discord Rich Presence" /B /MIN "%mypath%\MDRP\bin\Release\RunHidden.vbs"
EXIT
REM "%mypath%\MDRP\bin\Release\MDRP.exe" Shortcuts_Only
REM start "" "%mypath%\Shortcuts\Run MDRP Background.lnk"
REM exit
GOTO MENU
:LAUNCHDRP
call "%mypath%\MDRP\bin\Release\RunHidden.bat"
GOTO MENU
:KILLHIDDENDRP
call "%mypath%\MDRP\bin\Release\KillHidden.bat"
GOTO MENU
:LINKDRPGROOVE
call "%mypath%\MDRP\bin\Release\LinkWithGroove.bat"
GOTO MENU
:LINKDRPWMP
call "%mypath%\MDRP\bin\Release\LinkWithWMP.bat"
GOTO MENU
:LINKDRPSYSSTART
call "%mypath%\MDRP\bin\Release\LinkSystemStartAndMDRP.bat"
GOTO MENU
:UNLINKDRPGROOVE
call "%mypath%\MDRP\bin\Release\UnlinkFromGroove.bat"
GOTO MENU
:UNLINKDRPWMP
call "%mypath%\MDRP\bin\Release\UnlinkFromWMP.bat"
GOTO MENU
:UNLINKDRPSYSSTART
call "%mypath%\MDRP\bin\Release\UnlinkFromSystemStart.bat"
GOTO MENU
