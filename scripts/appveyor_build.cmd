:: @ECHO OFF

:: Restore packages
cd %APPVEYOR_BUILD_FOLDER%
dotnet restore --configfile nuget.config

:: Build packages
cd src\Steeltoe.CloudFoundry.Connector
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration Release)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration Release --version-suffix %STEELTOE_VERSION_SUFFIX%)
cd %APPVEYOR_BUILD_FOLDER%
cd src\Steeltoe.CloudFoundry.Connector.MySql
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration Release)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration Release --version-suffix %STEELTOE_VERSION_SUFFIX%)
cd %APPVEYOR_BUILD_FOLDER%
cd src\Steeltoe.CloudFoundry.Connector.Redis
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration Release)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration Release --version-suffix %STEELTOE_VERSION_SUFFIX%)
cd %APPVEYOR_BUILD_FOLDER%
cd src\Steeltoe.CloudFoundry.Connector.PostgreSql
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration Release)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration Release --version-suffix %STEELTOE_VERSION_SUFFIX%)
cd %APPVEYOR_BUILD_FOLDER%
cd src\Steeltoe.CloudFoundry.Connector.Rabbit
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration Release)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration Release --version-suffix %STEELTOE_VERSION_SUFFIX%)
cd %APPVEYOR_BUILD_FOLDER%
cd src\Steeltoe.CloudFoundry.Connector.OAuth
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration Release)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration Release --version-suffix %STEELTOE_VERSION_SUFFIX%)
cd %APPVEYOR_BUILD_FOLDER%
