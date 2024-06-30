REM @echo off

REM Set the name of the service executable
SET SERVICE_EXE=C:\Users\User\source\repos\NetServ\NetServ\bin\Debug\NetServ.exe

REM Check if the service executable exists
IF NOT EXIST "%SERVICE_EXE%" (
    echo Service executable %SERVICE_EXE% not found.
    echo Ensure that the executable is in the same directory as this script.
	pause
    exit /b 1
)

REM Install the service using InstallUtil.exe
echo Installing the service...
"C:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe" "C:\Users\User\source\repos\NetServ\NetServ\bin\Debug\NetServ.exe"

REM Check if the service was installed successfully
IF %ERRORLEVEL% EQU 0 (
    echo Service installed successfully.
    echo Starting the service...
    net start NetServ
	pause
) ELSE (
    echo Service installation failed.
	pause
    exit /b 1
)

REM Check if the service started successfully
IF %ERRORLEVEL% EQU 0 (
    echo Service started successfully.
) ELSE (
    echo Failed to start the service.
    exit /b 1
)

REM Pause to allow the user to see the output
pause
