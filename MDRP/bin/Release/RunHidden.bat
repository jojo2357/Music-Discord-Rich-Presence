@echo off

set directory=%~dp0
Pushd %directory%

echo %cd%

START /MIN "Rich Presence for Discord" MDRP

exit