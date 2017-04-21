@ECHO OFF

:: Run unit tests 
cd test\Steeltoe.Security.Authentication.CloudFoundry.Test
dotnet restore --configfile ..\..\nuget.config
dotnet test
if not "%errorlevel%"=="0" goto failure
cd ..\..

cd test\Steeltoe.Security.DataProtection.Redis.Test
dotnet restore --configfile ..\..\nuget.config
dotnet test
if not "%errorlevel%"=="0" goto failure
cd ..\..

echo Unit Tests Pass
goto success
:failure
echo Unit Tests Failure
exit -1
:success