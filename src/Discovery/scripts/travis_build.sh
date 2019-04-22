#!/bin/bash

echo Code is built in Unit Tests

cd src/Steeltoe.Discovery.EurekaBase
dotnet restore --configfile ../../nuget.config
cd ../..

cd src/Steeltoe.Discovery.ClientCore
dotnet restore --configfile ../../nuget.config
cd ../..

cd src/Steeltoe.Discovery.ClientAutofac
dotnet restore --configfile ../../nuget.config
cd ../..

