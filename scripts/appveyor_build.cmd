:: @ECHO OFF

:: Patch project.json files
cd %APPVEYOR_BUILD_FOLDER%\scripts
call npm install
call node patch-project-json.js ../src/Steeltoe.CloudFoundry.Connector/project.json %APPVEYOR_BUILD_VERSION% %APPVEYOR_REPO_TAG_NAME%
call node patch-project-json.js ../src/Steeltoe.CloudFoundry.Connector.MySql/project.json %APPVEYOR_BUILD_VERSION% %APPVEYOR_REPO_TAG_NAME%
call node patch-project-json.js ../src/Steeltoe.CloudFoundry.Connector.Redis/project.json %APPVEYOR_BUILD_VERSION% %APPVEYOR_REPO_TAG_NAME%
call node patch-project-json.js ../src/Steeltoe.CloudFoundry.Connector.PostgreSql/project.json %APPVEYOR_BUILD_VERSION% %APPVEYOR_REPO_TAG_NAME%
call node patch-project-json.js ../src/Steeltoe.CloudFoundry.Connector.Rabbit/project.json %APPVEYOR_BUILD_VERSION% %APPVEYOR_REPO_TAG_NAME%
call node patch-project-json.js ../src/Steeltoe.CloudFoundry.Connector.OAuth/project.json %APPVEYOR_BUILD_VERSION% %APPVEYOR_REPO_TAG_NAME%
call node patch-project-json.js ../test/Steeltoe.CloudFoundry.Connector.Test/project.json %APPVEYOR_BUILD_VERSION% %APPVEYOR_REPO_TAG_NAME%
call node patch-project-json.js ../test/Steeltoe.CloudFoundry.Connector.MySql.Test/project.json %APPVEYOR_BUILD_VERSION% %APPVEYOR_REPO_TAG_NAME%
call node patch-project-json.js ../test/Steeltoe.CloudFoundry.Connector.Redis.Test/project.json %APPVEYOR_BUILD_VERSION% %APPVEYOR_REPO_TAG_NAME%
call node patch-project-json.js ../test/Steeltoe.CloudFoundry.Connector.PostgreSql.Test/project.json %APPVEYOR_BUILD_VERSION% %APPVEYOR_REPO_TAG_NAME%
call node patch-project-json.js ../test/Steeltoe.CloudFoundry.Connector.Rabbit.Test/project.json %APPVEYOR_BUILD_VERSION% %APPVEYOR_REPO_TAG_NAME%
call node patch-project-json.js ../test/Steeltoe.CloudFoundry.Connector.OAuth.Test/project.json %APPVEYOR_BUILD_VERSION% %APPVEYOR_REPO_TAG_NAME%
cd %APPVEYOR_BUILD_FOLDER%

:: Restore packages
cd src
dotnet restore --disable-parallel
cd ..\test
dotnet restore --disable-parallel
cd ..

:: Build packages
cd src\Steeltoe.CloudFoundry.Connector
dotnet pack --configuration Release
cd %APPVEYOR_BUILD_FOLDER%
cd src\Steeltoe.CloudFoundry.Connector.MySql
dotnet pack --configuration Release
cd %APPVEYOR_BUILD_FOLDER%
cd src\Steeltoe.CloudFoundry.Connector.Redis
dotnet pack --configuration Release
cd %APPVEYOR_BUILD_FOLDER%
cd src\Steeltoe.CloudFoundry.Connector.PostgreSql
dotnet pack --configuration Release
cd %APPVEYOR_BUILD_FOLDER%
cd src\Steeltoe.CloudFoundry.Connector.Rabbit
dotnet pack --configuration Release
cd %APPVEYOR_BUILD_FOLDER%
cd src\Steeltoe.CloudFoundry.Connector.OAuth
dotnet pack --configuration Release
cd %APPVEYOR_BUILD_FOLDER%
