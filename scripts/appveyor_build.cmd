:: @ECHO OFF

:: Restore packages
cd src
dotnet restore
cd ..\test
dotnet restore
cd ..

:: Build packages
cd src\Steeltoe.Extensions.Configuration.CloudFoundry
IF %APPVEYOR_REPO_TAG%=="true" (dotnet pack --configuration Release)
IF %APPVEYOR_REPO_TAG%=="false" (dotnet pack --configuration Release --version-suffix %STEELTOE_VERSION_SUFFIX%)
cd %APPVEYOR_BUILD_FOLDER%
cd src\Steeltoe.Extensions.Configuration.ConfigServer
IF %APPVEYOR_REPO_TAG%=="true" (dotnet pack --configuration Release)
IF %APPVEYOR_REPO_TAG%=="false" (dotnet pack --configuration Release --version-suffix %STEELTOE_VERSION_SUFFIX%)
cd %APPVEYOR_BUILD_FOLDER%
