@ECHO OFF

:: Run unit tests 
cd test\SteelToe.Security.Authentication.CloudFoundry.Test
dotnet test
cd ..\..
