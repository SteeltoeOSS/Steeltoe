:: @ECHO OFF
:: Target (x64 CLR)
call dnvm use 1.0.0-rc1-update1 -a x64 -r clr

:: Patch project.json files
cd %APPVEYOR_BUILD_FOLDER%\scripts
call npm install
call node patch-project-json.js ../src/SteelToe.Discovery.Client/project.json %APPVEYOR_BUILD_VERSION% %APPVEYOR_REPO_TAG_NAME%
call node patch-project-json.js ../src/SteelToe.Discovery.Eureka.Client/project.json %APPVEYOR_BUILD_VERSION% %APPVEYOR_REPO_TAG_NAME%
cd %APPVEYOR_BUILD_FOLDER%

:: Restore packages
cd src
call dnu restore
cd ..\test
call dnu restore
cd ..

:: Build packages
cd src\SteelToe.Discovery.Client
call dnu pack --configuration Release
cd %APPVEYOR_BUILD_FOLDER%
cd src\SteelToe.Discovery.Eureka.Client
call dnu pack --configuration Release
cd %APPVEYOR_BUILD_FOLDER%
