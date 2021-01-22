@echo off

taskkill /im GroovyRP.exe

set directory=%~dp0
Pushd %directory%

echo %cd%

START /MIN "Rich Presence for Discord" GroovyRP 