@ECHO OFF

:: Run unit tests 
cd test\Steeltoe.Extensions.Configuration.CloudFoundryBase.Test
dotnet restore --configfile ..\..\nuget.config
dotnet xunit -verbose 
if not "%errorlevel%"=="0" goto failure
cd ..\..

cd test\Steeltoe.Extensions.Configuration.CloudFoundryCore.Test
dotnet restore --configfile ..\..\nuget.config
dotnet xunit -verbose 
if not "%errorlevel%"=="0" goto failure
cd ..\..

cd test\Steeltoe.Extensions.Configuration.CloudFoundryAutofac.Test
dotnet restore --configfile ..\..\nuget.config
dotnet xunit -verbose 
if not "%errorlevel%"=="0" goto failure
cd ..\..

cd test\Steeltoe.Extensions.Configuration.ConfigServerBase.Test
dotnet restore --configfile ..\..\nuget.config
dotnet xunit -verbose 
if not "%errorlevel%"=="0" goto failure
cd ..\..

cd test\Steeltoe.Extensions.Configuration.ConfigServerCore.Test
dotnet restore --configfile ..\..\nuget.config
dotnet xunit -verbose 
if not "%errorlevel%"=="0" goto failure
cd ..\..

cd test\Steeltoe.Extensions.Configuration.ConfigServerAutofac.Test
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