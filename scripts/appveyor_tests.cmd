@ECHO OFF

:: Run unit tests 
cd test\SteelToe.CloudFoundry.Connector.Test
dotnet test
cd ..\..
cd test\SteelToe.CloudFoundry.Connector.MySql.Test
dotnet test
cd ..\..
cd test\SteelToe.CloudFoundry.Connector.Redis.Test
dotnet test
cd ..\..
