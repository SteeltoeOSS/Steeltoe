@ECHO OFF

:: Run unit tests 
cd test\SteelToe.Discovery.Client.Test
dotnet test
cd ..\..
cd test\SteelToe.Discovery.Eureka.Client.Test
dotnet test
cd ..\..