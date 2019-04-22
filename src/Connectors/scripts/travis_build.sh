#!/bin/bash

echo Code is built in Unit Tests


cd src/Steeltoe.CloudFoundry.ConnectorBase
dotnet restore --configfile ../../nuget.config
cd ../..

cd src/Steeltoe.CloudFoundry.Connector.EFCore
dotnet restore --configfile ../../nuget.config
cd ../..

cd src/Steeltoe.CloudFoundry.ConnectorCore
dotnet restore --configfile ../../nuget.config
cd ../..

cd src/Steeltoe.CloudFoundry.ConnectorAutofac
dotnet restore --configfile ../../nuget.config
cd ../..
