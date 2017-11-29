@echo off
SET THISNAME=iedup
REM installutil %THISNAME%.exe
SET THIS_PATH=%~dp0
IF EXIST "%THIS_PATH%\bin\Release\%THISNAME%.exe" "%THIS_PATH%\bin\Release\%THISNAME%.exe" -install
IF NOT EXIST "%THIS_PATH%\bin\Release\%THISNAME%.exe" "%THIS_PATH%\bin\Debug\%THISNAME%.exe" -install
echo Normally (after implemented), this install and the following steps would be done by the iedusm service.
echo Now to go services.msc and do the following:
echo * change Startup type to "Start automatically"
echo * in "Log On" tab:
echo   * change "Log on as" to "Local System account"
echo   * "Allow service to interact with the desktop": yes
echo * OK, right-click the service, Start.
pause