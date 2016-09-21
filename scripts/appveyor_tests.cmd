@ECHO OFF

:: Run unit tests 
cd test\Steeltoe.Security.Authentication.CloudFoundry.Test
dotnet test
cd ..\..
