#!/bin/bash

echo Code is built in Unit Tests

cd src/Steeltoe.Managment.Endpoint
dotnet restore --configfile ../../nuget.config
cd ../..
cd src/Steeltoe.Managment.Endpoint.CloudFoundry
dotnet restore --configfile ../../nuget.config
cd ../..
cd src/Steeltoe.Managment.Endpoint.Health
dotnet restore --configfile ../../nuget.config
cd ../..
cd src/Steeltoe.Managment.Endpoint.Info
dotnet restore --configfile ../../nuget.config
cd ../..
cd src/Steeltoe.Managment.Endpoint.Loggers
dotnet restore --configfile ../../nuget.config
cd ../..
cd src/Steeltoe.Managment.Endpoint.Trace
dotnet restore --configfile ../../nuget.config
cd ../..
cd src/Steeltoe.Managment.CloudFoundry
dotnet restore --configfile ../../nuget.config
cd ../..
