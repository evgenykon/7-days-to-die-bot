@echo off
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Requesting admin privileges...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

echo Reserving URL http://+:9090/ ...
netsh http add urlacl url=http://+:9090/ sddl=D:(A;;GA;;;WD)
if %errorlevel% equ 0 (
    echo Done. URL reserved successfully.
) else (
    echo Trying with user name...
    netsh http add urlacl url=http://+:9090/ user="NT AUTHORITY\NETWORK SERVICE"
)
pause
