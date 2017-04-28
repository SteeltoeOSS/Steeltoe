#!/bin/bash

echo Code is built in Unit Tests


cd src/Steeltoe.Extensions.Configuration.CloudFoundry
dotnet restore --configfile ../../nuget.config
cd ../..

cd src/Steeltoe.Extensions.Configuration.ConfigServer
dotnet restore --configfile ../../nuget.config
cd ../..
