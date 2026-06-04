@echo off
cd /d "%~dp0"
echo Stopping Companion Bot Server...
taskkill /F /IM bun.exe 2>nul
