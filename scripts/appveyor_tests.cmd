@ECHO OFF

:: Run unit tests 
cd test\Steeltoe.Extensions.Configuration.CloudFoundry.Test
dotnet test
cd ..\..
cd test\Steeltoe.Extensions.Configuration.ConfigServer.Test
dotnet test
cd ..\..
