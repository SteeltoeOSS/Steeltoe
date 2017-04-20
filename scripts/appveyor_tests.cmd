@ECHO OFF

:: Run unit tests 
cd test\Steeltoe.Extensions.Configuration.CloudFoundry.Test
dotnet test
if not "%errorlevel%"=="0" goto failure
cd ..\..
cd test\Steeltoe.Extensions.Configuration.ConfigServer.Test
dotnet test
if not "%errorlevel%"=="0" goto failure
cd ..\..
echo Unit Tests Pass
goto success
:failure
echo Unit Tests Failure
exit -1
:success