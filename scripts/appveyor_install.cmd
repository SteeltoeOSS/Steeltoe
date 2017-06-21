@ECHO OFF
:: Output dotnet info
dotnet --info

SET CI_BUILD=%APPVEYOR%
SET number=00000%APPVEYOR_BUILD_NUMBER%
SET STEELTOE_VERSION=1.1.0
SET STEELTOE_VERSION_SUFFIX=%APPVEYOR_REPO_BRANCH%-%number:~-5%
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" SET STEELTOE_VERSION_SUFFIX=%APPVEYOR_REPO_TAG_NAME:~6,5%
IF NOT "%STEELTOE_VERSION_SUFFIX%"=="" (
    SET STEELTOE_VERSION_SUFFIX=%STEELTOE_VERSION_SUFFIX: =%
    SET STEELTOE_DASH_VERSION_SUFFIX=-%STEELTOE_VERSION_SUFFIX%
) ELSE (
    SET STEELTOE_DASH_VERSION_SUFFIX=
)   
echo "Version Suffix:" %STEELTOE_VERSION_SUFFIX%
SET BUILD_TYPE=Release
IF "%APPVEYOR_REPO_BRANCH%"=="master" COPY config\nuget-master.config .\nuget.config
IF "%APPVEYOR_REPO_BRANCH%"=="dev" COPY config\nuget-dev.config .\nuget.config
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" COPY config\nuget.config .\nuget.config
IF "%APPVEYOR_REPO_BRANCH%"=="master" COPY config\versions-master.props .\versions.props
IF "%APPVEYOR_REPO_BRANCH%"=="dev" COPY config\versions-dev.props .\versions.props
IF NOT "%APPVEYOR_REPO_TAG_NAME%"=="" COPY config\versions.props .\versions.props
IF "%APPVEYOR_REPO_BRANCH%"=="dev" SET BUILD_TYPE=Debug
mkdir %USERPROFILE%\localfeed
nuget sources add -Name localfeed -Source %USERPROFILE%\localfeed -ConfigFile .\nuget.config
