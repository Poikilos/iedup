SET THISNAME=NativeWifi
SET OTHERNAME=ManagedWifi
IF EXIST "..\..\bin\Release\%OTHERNAME%.dll" del "..\..\bin\Release\%OTHERNAME%.dll"
IF EXIST "..\..\bin\Debug\%OTHERNAME%.dll" del "..\..\bin\Debug\%OTHERNAME%.dll"
IF EXIST "..\..\bin\Debug\%OTHERNAME%.pdb" del "..\..\bin\Debug\%OTHERNAME%.pdb"
IF EXIST "..\..\bin\Release\%THISNAME%.dll" del "..\..\bin\Release\%THISNAME%.dll"
IF EXIST "..\..\bin\Debug\%THISNAME%.dll" del "..\..\bin\Debug\%THISNAME%.dll"
IF EXIST "..\..\bin\Debug\%THISNAME%.pdb" del "..\..\bin\Debug\%THISNAME%.pdb"
copy %THISNAME%.dll ..\..\bin\Release\
copy %THISNAME%.dll ..\..\bin\Debug\
copy %THISNAME%.pdb ..\..\bin\Debug\



