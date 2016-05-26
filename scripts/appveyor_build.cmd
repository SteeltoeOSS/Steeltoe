:: @ECHO OFF

:: Patch project.json files
cd %APPVEYOR_BUILD_FOLDER%\scripts
call npm install
call node patch-project-json.js ../src/SteelToe.Extensions.Configuration.CloudFoundry/project.json %APPVEYOR_BUILD_VERSION% %APPVEYOR_REPO_TAG_NAME%
call node patch-project-json.js ../src/SteelToe.Extensions.Configuration.ConfigServer/project.json %APPVEYOR_BUILD_VERSION% %APPVEYOR_REPO_TAG_NAME%
cd %APPVEYOR_BUILD_FOLDER%

:: Restore packages
cd src
dotnet restore
cd ..\test
dotnet restore
cd ..

:: Build packages
cd src\SteelToe.Extensions.Configuration.CloudFoundry
dotnet pack --configuration Release
cd %APPVEYOR_BUILD_FOLDER%
cd src\SteelToe.Extensions.Configuration.ConfigServer
dotnet pack --configuration Release
cd %APPVEYOR_BUILD_FOLDER%
