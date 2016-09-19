:: @ECHO OFF

:: Patch project.json files
cd %APPVEYOR_BUILD_FOLDER%\scripts
call npm install
call node patch-project-json.js ../src/Steeltoe.Discovery.Client/project.json %APPVEYOR_BUILD_VERSION% %APPVEYOR_REPO_TAG_NAME%
call node patch-project-json.js ../src/Steeltoe.Discovery.Eureka.Client/project.json %APPVEYOR_BUILD_VERSION% %APPVEYOR_REPO_TAG_NAME%
cd %APPVEYOR_BUILD_FOLDER%

:: Restore packages
cd src
dotnet restore
cd ..\test
dotnet restore
cd ..

:: Build packages
cd src\Steeltoe.Discovery.Client
dotnet pack --configuration Release
cd %APPVEYOR_BUILD_FOLDER%
cd src\Steeltoe.Discovery.Eureka.Client
dotnet pack --configuration Release
cd %APPVEYOR_BUILD_FOLDER%
