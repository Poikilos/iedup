@echo off
SET THISNAME=iedup
REM installutil %THISNAME%.exe
SET THIS_PATH=%~dp0
IF EXIST "%THIS_PATH%\bin\Release\%THISNAME%.exe" "%THIS_PATH%\bin\Release\%THISNAME%.exe" -uninstall
IF NOT EXIST "%THIS_PATH%\bin\Release\%THISNAME%.exe" "%THIS_PATH%\bin\Debug\%THISNAME%.exe" -uninstall
pause