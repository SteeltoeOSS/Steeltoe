:: @ECHO OFF
:: Target (x64 CLR)
call dnvm use 1.0.0-rc1-update1 -a x64 -r clr

:: Patch project.json files
cd %APPVEYOR_BUILD_FOLDER%\build
call npm install
call node patch-project-json.js ../src/SteelToe.Extensions.Configuration.CloudFoundry/project.json %APPVEYOR_BUILD_VERSION% %APPVEYOR_REPO_TAG_NAME%
call node patch-project-json.js ../src/SteelToe.Extensions.Configuration.ConfigServer/project.json %APPVEYOR_BUILD_VERSION% %APPVEYOR_REPO_TAG_NAME%
cd %APPVEYOR_BUILD_FOLDER%

:: Restore packages
cd src
call dnu restore
cd ..\test
call dnu restore
cd ..

:: Build packages
cd src\SteelToe.Extensions.Configuration.CloudFoundry
call dnu pack --configuration Release
cd %APPVEYOR_BUILD_FOLDER%
cd src\SteelToe.Extensions.Configuration.ConfigServer
call dnu pack --configuration Release
cd %APPVEYOR_BUILD_FOLDER%
