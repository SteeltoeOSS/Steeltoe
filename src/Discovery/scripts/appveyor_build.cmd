:: @ECHO OFF

:: Build packages
cd src\Steeltoe.Discovery.Eureka.Client
dotnet restore --configfile ..\..\nuget.config
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (
    IF NOT "%STEELTOE_VERSION_SUFFIX%"=="" (
        dotnet pack --configuration %BUILD_TYPE% --version-suffix %STEELTOE_VERSION_SUFFIX%
        nuget add "bin\%BUILD_TYPE%\Steeltoe.Discovery.Eureka.Client.%STEELTOE_VERSION%-%STEELTOE_VERSION_SUFFIX%.nupkg" -Source "%USERPROFILE%\localfeed"
    ) ELSE (
        dotnet pack --configuration %BUILD_TYPE%
        nuget add "bin\%BUILD_TYPE%\Steeltoe.Discovery.Eureka.Client.%STEELTOE_VERSION%.nupkg" -Source "%USERPROFILE%\localfeed"
    )    
)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration %BUILD_TYPE% --version-suffix %STEELTOE_VERSION_SUFFIX% --include-symbols --include-source)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (nuget add -Source %USERPROFILE%\localfeed bin\%BUILD_TYPE%\Steeltoe.Discovery.Eureka.Client.%STEELTOE_VERSION%-%STEELTOE_VERSION_SUFFIX%.nupkg)
cd ..\..

cd src\Steeltoe.Discovery.Client
dotnet restore --configfile ..\..\nuget.config
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" (
    IF NOT "%STEELTOE_VERSION_SUFFIX%"=="" (
        dotnet pack --configuration %BUILD_TYPE% --version-suffix %STEELTOE_VERSION_SUFFIX%
    ) ELSE (
        dotnet pack --configuration %BUILD_TYPE%
    )    
)
IF "%APPVEYOR_REPO_TAG_NAME%"=="" (dotnet pack --configuration %BUILD_TYPE% --version-suffix %STEELTOE_VERSION_SUFFIX% --include-symbols --include-source)
cd ..\..