@ECHO OFF

:: Run unit tests 
cd test\Steeltoe.CloudFoundry.Connector.Test
dotnet restore --configfile ..\..\nuget.config
dotnet xunit -verbose
if not "%errorlevel%"=="0" goto failure
cd ..\..

:: cd test\Steeltoe.CloudFoundry.Connector.MySql.Test
:: dotnet restore --configfile ..\..\nuget.config
:: dotnet xunit -verbose
:: if not "%errorlevel%"=="0" goto failure
:: cd ..\..

cd test\Steeltoe.CloudFoundry.Connector.Redis.Test
dotnet restore --configfile ..\..\nuget.config
dotnet xunit -verbose
if not "%errorlevel%"=="0" goto failure
cd ..\..

cd test\Steeltoe.CloudFoundry.Connector.PostgreSql.Test
dotnet restore --configfile ..\..\nuget.config
dotnet xunit -verbose
if not "%errorlevel%"=="0" goto failure
cd ..\..

cd test\Steeltoe.CloudFoundry.Connector.Rabbit.Test
dotnet restore --configfile ..\..\nuget.config
dotnet xunit -verbose
if not "%errorlevel%"=="0" goto failure
cd ..\..

cd test\Steeltoe.CloudFoundry.Connector.OAuth.Test
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