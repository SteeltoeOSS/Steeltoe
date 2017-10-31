@ECHO OFF

:: Run unit tests 
cd test\Steeltoe.Management.Endpoint.Test
dotnet restore --configfile ..\..\nuget.config
dotnet xunit -verbose
if not "%errorlevel%"=="0" goto failure
cd ..\..
cd test\Steeltoe.Management.CloudFoundry.Test
dotnet restore --configfile ..\..\nuget.config
dotnet xunit -verbose
if not "%errorlevel%"=="0" goto failure
cd ..\..
cd test\Steeltoe.Management.CloudFoundryCore.Test
dotnet restore --configfile ..\..\nuget.config
dotnet xunit -verbose
if not "%errorlevel%"=="0" goto failure
cd ..\..
echo Unit Tests Pass
goto success
:failure
echo Unit Tests Failure
exit -1
:success