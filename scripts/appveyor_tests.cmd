@ECHO OFF

:: Run unit tests 
cd test\Steeltoe.Discovery.Client.Test
dotnet test
cd ..\..
cd test\Steeltoe.Discovery.Eureka.Client.Test
dotnet test
cd ..\..
