DESKTOP_USER=%USER%
PROJECTS_PATH=C:\Users\%USER%\Documents\GitHub
IF EXIST "C:\Users\%USER%\GitHub" PROJECTS_PATH=C:\Users\%USER%\GitHub
REM IF NOT EXIST "%PROJECTS_PATH%" THEN GOTO END_ERROR
IF EXIST "%PROJECTS_PATH%" cd "%PROJECTS_PATH%"
REM cd just so fewer location checks are needed (only one location) for iedup.exe's location:
IF EXIST iedup cd iedup
REM else assume this file was run from the right place (the iedup project folder)
IF NOT EXIST ".\bin\Release\iedup.exe" echo echo you must build a iedup release (Get IDE such as SharpDevelop, open iedup.sln in it, Build, Set Configuration, 'Release', then Build, Build Solution)
IF NOT EXIST ".\bin\Release\iedup.exe" GOTO END_ERROR 
IF NOT EXIST "%PROGRAMFILES%\iedup" md "%PROGRAMFILES%\iedup"
IF NOT EXIST "%PROGRAMFILES%\iedup" echo You must be administrator to run this (directory %PROGRAMFILES%\iedup could not be created).
IF NOT EXIST "%PROGRAMFILES%\iedup" GOTO END_ERROR
IF NOT EXIST "iedusm" THEN echo You must first clone iedusm from https://github.com/expertmm/iedusm.git such as using GitHub Desktop
IF NOT EXIST "iedusm" THEN GOTO END_ERROR
cd iedusm
IF NOT EXIST ".\bin\Release\iedusm.exe" echo you must build a iedusm release too (Get IDE such as SharpDevelop, open iedusm.sln in it, Build, Set Configuration, 'Release', then Build, Build Solution)
IF NOT EXIST ".\bin\Release\iedusm.exe" THEN GOTO END_ERROR
cd ".\bin\Release\"
echo "About to run iedusm.exe which will setup iedup and install it as a service..."
iedusm.exe -install_managed_software
GOTO END_SILENT

:END_ERROR
echo Installation failed.
:END_SILENT
pause