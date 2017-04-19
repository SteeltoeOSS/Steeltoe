:: @ECHO OFF

:: Restore packages
cd %APPVEYOR_BUILD_FOLDER%
dotnet restore

:: Build packages
cd src\Steeltoe.Extensions.Configuration.CloudFoundry
IF %APPVEYOR_REPO_TAG%=="true" (dotnet pack --configuration Release)
IF %APPVEYOR_REPO_TAG%=="false" (dotnet pack --configuration Release --version-suffix %STEELTOE_VERSION_SUFFIX%)
cd %APPVEYOR_BUILD_FOLDER%
cd src\Steeltoe.Extensions.Configuration.ConfigServer
IF %APPVEYOR_REPO_TAG%=="true" (dotnet pack --configuration Release)
IF %APPVEYOR_REPO_TAG%=="false" (dotnet pack --configuration Release --version-suffix %STEELTOE_VERSION_SUFFIX%)
cd %APPVEYOR_BUILD_FOLDER%
