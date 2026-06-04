@echo off
start /B /MIN "" "%~dp0PiperServer.exe" > "%~dp0piper-server.log" 2>&1
echo PiperServer started on port 9092 (PID: unknown)
echo Log: piper-server.log
