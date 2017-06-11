:: @ECHO OFF

:: Build packages
cd src\Steeltoe.CircuitBreaker.Hystrix.Core
dotnet restore --configfile ..\..\nuget.config
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration %BUILD_TYPE%)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration %BUILD_TYPE% --version-suffix %STEELTOE_VERSION_SUFFIX% --include-symbols --include-source)
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (nuget add -source %USERPROFILE%\localfeed bin\%BUILD_TYPE%\Steeltoe.CircuitBreaker.Hystrix.Core.%STEELTOE_VERSION%.nupkg)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (nuget add -source %USERPROFILE%\localfeed bin\%BUILD_TYPE%\Steeltoe.CircuitBreaker.Hystrix.Core.%STEELTOE_VERSION%-%STEELTOE_VERSION_SUFFIX%.nupkg)
cd %APPVEYOR_BUILD_FOLDER%
cd src\Steeltoe.CircuitBreaker.Hystrix
dotnet restore --configfile ..\..\nuget.config
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration %BUILD_TYPE%)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration %BUILD_TYPE% --version-suffix %STEELTOE_VERSION_SUFFIX% --include-symbols --include-source)
cd %APPVEYOR_BUILD_FOLDER%
cd src\Steeltoe.CircuitBreaker.Hystrix.MetricsEvents
dotnet restore --configfile ..\..\nuget.config
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration %BUILD_TYPE%)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration %BUILD_TYPE% --version-suffix %STEELTOE_VERSION_SUFFIX% --include-symbols --include-source)
cd %APPVEYOR_BUILD_FOLDER%
cd src\Steeltoe.CircuitBreaker.Hystrix.MetricsStream
dotnet restore --configfile ..\..\nuget.config
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration %BUILD_TYPE%)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration %BUILD_TYPE% --version-suffix %STEELTOE_VERSION_SUFFIX% --include-symbols --include-source)
cd %APPVEYOR_BUILD_FOLDER%