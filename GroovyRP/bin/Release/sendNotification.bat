@echo off
echo Summoned with the args: %1 %2
call :notif %1 %2
goto:eof

:notif
::Syntaxe : call :notif "Titre" "Message"

set type=Information
set "$Titre=%~1"
Set "$Message=%~2"

::You can replace the $Icon value by Information, error, warning and none
Set "$Icon=Information"
for /f "delims=" %%a in ('powershell -c "[reflection.assembly]::loadwithpartialname('System.Windows.Forms');[reflection.assembly]::loadwithpartialname('System.Drawing');$notify = new-object system.windows.forms.notifyicon;$notify.icon = [System.Drawing.SystemIcons]::%$Icon%;$notify.visible = $true;$notify.showballoontip(10,'%$Titre%','%$Message%',[system.windows.forms.tooltipicon]::None)"') do (set $=)

goto:eof