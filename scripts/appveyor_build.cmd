:: @ECHO OFF

:: Build packages
cd src\Steeltoe.CloudFoundry.Connector
dotnet restore --configfile ..\..\nuget.config
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration %BUILD_TYPE%)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration %BUILD_TYPE% --version-suffix %STEELTOE_VERSION_SUFFIX% --include-symbols --include-source)
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (nuget add -source %USERPROFILE%\localfeed bin\%BUILD_TYPE%\Steeltoe.CloudFoundry.Connector.%STEELTOE_VERSION%.nupkg)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (nuget add -source %USERPROFILE%\localfeed bin\%BUILD_TYPE%.\Steeltoe.CloudFoundry.Connector.%STEELTOE_VERSION%-%STEELTOE_VERSION_SUFFIX%.nupkg)
cd %APPVEYOR_BUILD_FOLDER%

cd src\Steeltoe.CloudFoundry.Connector.MySql
dotnet restore --configfile ..\..\nuget.config
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration %BUILD_TYPE%)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration %BUILD_TYPE% --version-suffix %STEELTOE_VERSION_SUFFIX% --include-symbols --include-source)
cd %APPVEYOR_BUILD_FOLDER%

cd src\Steeltoe.CloudFoundry.Connector.Redis
dotnet restore --configfile ..\..\nuget.config
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration %BUILD_TYPE%)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration %BUILD_TYPE% --version-suffix %STEELTOE_VERSION_SUFFIX% --include-symbols --include-source)
cd %APPVEYOR_BUILD_FOLDER%

cd src\Steeltoe.CloudFoundry.Connector.PostgreSql
dotnet restore --configfile ..\..\nuget.config
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration %BUILD_TYPE%)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration %BUILD_TYPE% --version-suffix %STEELTOE_VERSION_SUFFIX% --include-symbols --include-source)
cd %APPVEYOR_BUILD_FOLDER%

cd src\Steeltoe.CloudFoundry.Connector.Rabbit
dotnet restore --configfile ..\..\nuget.config
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration %BUILD_TYPE%)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration %BUILD_TYPE% --version-suffix %STEELTOE_VERSION_SUFFIX% --include-symbols --include-source)
cd %APPVEYOR_BUILD_FOLDER%

cd src\Steeltoe.CloudFoundry.Connector.OAuth
dotnet restore --configfile ..\..\nuget.config
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration %BUILD_TYPE%)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration %BUILD_TYPE% --version-suffix %STEELTOE_VERSION_SUFFIX% --include-symbols --include-source)
cd %APPVEYOR_BUILD_FOLDER%

cd src\Steeltoe.CloudFoundry.Connector.Hystrix
dotnet restore --configfile ..\..\nuget.config
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration %BUILD_TYPE%)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration %BUILD_TYPE% --version-suffix %STEELTOE_VERSION_SUFFIX% --include-symbols --include-source)
cd %APPVEYOR_BUILD_FOLDER%