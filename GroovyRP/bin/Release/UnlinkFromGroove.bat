@echo off

echo Checking admin rights...
net session>nul

cls

if not '%ERRORLEVEL%'=='0' (
echo Admin rights not detected. Please run with admin perms
pause
EXIT /B
)

schtasks /delete /tn GrooveRichPresenceOpen
schtasks /delete /tn GrooveRichPresenceClose

pause 