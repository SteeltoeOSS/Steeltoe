@ECHO OFF
:: Output dotnet info
dotnet --info
:: For patching project.json's
call npm install jsonfile -g
