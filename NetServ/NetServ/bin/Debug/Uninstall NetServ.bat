REM @echo off

REM Set the name of the service executable with the full file path and executable name
SET SERVICE_EXE=C:\Users\User\source\repos\NetServ\NetServ\bin\Debug\NetServ.exe

REM Check if the service executable exists
IF NOT EXIST "%SERVICE_EXE%" (
    echo Service executable %SERVICE_EXE% not found.
    echo Ensure that the executable is in the same directory as this script.
	pause
    exit /b 1
)

IF EXIST "%SERVICE_EXE%" (
    echo Stopping the service...
    net stop NetServ
    echo Service stopped successfully.
	echo Uninstalling the service...
	"C:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe" /u "C:\Users\User\source\repos\NetServ\NetServ\bin\Debug\NetServ.exe"
	echo NetServ executable has successfully been uninstalled.
	pause
    exit /b 1
)

REM Pause to allow the user to see the output
pause
