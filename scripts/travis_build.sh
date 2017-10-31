#!/bin/bash

echo Code is built in Unit Tests

cd src/Steeltoe.Management.Endpoint
dotnet restore --configfile ../../nuget.config
cd ../..
cd src/Steeltoe.Management.EndpointCore
dotnet restore --configfile ../../nuget.config
cd ../..
cd src/Steeltoe.Management.CloudFoundry
dotnet restore --configfile ../../nuget.config
cd ../..
cd src/Steeltoe.Management.CloudFoundryCore
dotnet restore --configfile ../../nuget.config
cd ../..
