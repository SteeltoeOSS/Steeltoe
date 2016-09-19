@ECHO OFF

:: Run unit tests 
cd test\Steeltoe.CloudFoundry.Connector.Test
dotnet test
cd ..\..
cd test\Steeltoe.CloudFoundry.Connector.MySql.Test
dotnet test
cd ..\..
cd test\Steeltoe.CloudFoundry.Connector.Redis.Test
dotnet test
cd ..\..
cd test\Steeltoe.CloudFoundry.Connector.PostgreSql.Test
dotnet test
cd ..\..
cd test\Steeltoe.CloudFoundry.Connector.Rabbit.Test
dotnet test
cd ..\..
cd test\Steeltoe.CloudFoundry.Connector.OAuth.Test
dotnet test
cd ..\..
