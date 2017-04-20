:: @ECHO OFF

:: Restore packages
cd %APPVEYOR_BUILD_FOLDER%
dotnet restore --configfile nuget.config

:: Build packages
cd src\Steeltoe.Extensions.Configuration.CloudFoundry
IF NOT %APPVEYOR_REPO_TAG_NAME%=="" (dotnet pack --configuration Release)
IF %APPVEYOR_REPO_TAG_NAME%=="" (dotnet pack --configuration Release --version-suffix %STEELTOE_VERSION_SUFFIX%)
cd %APPVEYOR_BUILD_FOLDER%
cd src\Steeltoe.Extensions.Configuration.ConfigServer
IF NOT %APPVEYOR_REPO_TAG_NAME%=="" (dotnet pack --configuration Release)
IF %APPVEYOR_REPO_TAG_NAME%=="" (dotnet pack --configuration Release --version-suffix %STEELTOE_VERSION_SUFFIX%)
cd %APPVEYOR_BUILD_FOLDER%
