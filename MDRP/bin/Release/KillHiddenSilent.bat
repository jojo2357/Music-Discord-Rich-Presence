@echo off

curl -X POST --connect-timeout 0.05 -d "{message:\"please die\"}" localhost:2357>nul 2>&1

exit /b 0