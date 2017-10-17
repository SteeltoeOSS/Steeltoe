@ECHO OFF

:: Run unit tests 
cd test\Steeltoe.Discovery.ClientCore.Test
dotnet restore --configfile ..\..\nuget.config
dotnet xunit -verbose
if not "%errorlevel%"=="0" goto failure
cd ..\..

cd test\Steeltoe.Discovery.Eureka.Client.Test
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