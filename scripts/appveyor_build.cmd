:: @ECHO OFF

:: Build packages
cd src\Steeltoe.CloudFoundry.Connector
dotnet restore --configfile ..\..\nuget.config
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration Release)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration Release --version-suffix %STEELTOE_VERSION_SUFFIX%)
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (nuget add -source %USERPROFILE%\localfeed bin\Release\Steeltoe.CloudFoundry.Connector.%STEELTOE_VERSION%.nupkg)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (nuget add -source %USERPROFILE%\localfeed bin\Release\Steeltoe.CloudFoundry.Connector.%STEELTOE_VERSION%-%STEELTOE_VERSION_SUFFIX%.nupkg)
cd %APPVEYOR_BUILD_FOLDER%

cd src\Steeltoe.CloudFoundry.Connector.MySql
dotnet restore --configfile ..\..\nuget.config
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration Release)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration Release --version-suffix %STEELTOE_VERSION_SUFFIX%)
cd %APPVEYOR_BUILD_FOLDER%

cd src\Steeltoe.CloudFoundry.Connector.Redis
dotnet restore --configfile ..\..\nuget.config
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration Release)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration Release --version-suffix %STEELTOE_VERSION_SUFFIX%)
cd %APPVEYOR_BUILD_FOLDER%

cd src\Steeltoe.CloudFoundry.Connector.PostgreSql
dotnet restore --configfile ..\..\nuget.config
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration Release)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration Release --version-suffix %STEELTOE_VERSION_SUFFIX%)
cd %APPVEYOR_BUILD_FOLDER%

cd src\Steeltoe.CloudFoundry.Connector.Rabbit
dotnet restore --configfile ..\..\nuget.config
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration Release)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration Release --version-suffix %STEELTOE_VERSION_SUFFIX%)
cd %APPVEYOR_BUILD_FOLDER%

cd src\Steeltoe.CloudFoundry.Connector.OAuth
dotnet restore --configfile ..\..\nuget.config
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration Release)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration Release --version-suffix %STEELTOE_VERSION_SUFFIX%)
cd %APPVEYOR_BUILD_FOLDER%
