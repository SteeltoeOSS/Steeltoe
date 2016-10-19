@ECHO OFF

:: Run unit tests 
cd test\Steeltoe.Security.Authentication.CloudFoundry.Test
dotnet test
cd ..\..
cd test\Steeltoe.Security.DataProtection.Redis.Test
dotnet test
cd ..\..
