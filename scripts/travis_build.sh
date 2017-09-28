#!/bin/bash

echo Code is built in Unit Tests


cd src/Steeltoe.CloudFoundry.Connector
dotnet restore --configfile ../../nuget.config
cd ../..

cd src/Steeltoe.CloudFoundry.Connector.PostgreSql
dotnet restore --configfile ../../nuget.config
cd ../..

cd src/Steeltoe.CloudFoundry.Connector.Rabbit
dotnet restore --configfile ../../nuget.config
cd ../..

cd src/Steeltoe.CloudFoundry.Connector.OAuth
dotnet restore --configfile ../../nuget.config
cd ../..

cd src/Steeltoe.CloudFoundry.Connector.MySql
dotnet restore --configfile ../../nuget.config
cd ../..

cd src/Steeltoe.CloudFoundry.Connector.Redis
dotnet restore --configfile ../../nuget.config
cd ../..

cd src/Steeltoe.CloudFoundry.Connector.Hystrix
dotnet restore --configfile ../../nuget.config
cd ../..

cd src/Steeltoe.CloudFoundry.Connector.SqlServer
dotnet restore --configfile ../../nuget.config
cd ../..
