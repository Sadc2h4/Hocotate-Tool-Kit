@echo off
setlocal
set "EXE=%~dp0Hocotate_Toolkit.exe"

if not exist "%EXE%" (
    echo Hocotate_Toolkit.exe was not found next to this batch file.
    pause
    exit /b 1
)

"%EXE%" --unregister
exit /b %ERRORLEVEL%
