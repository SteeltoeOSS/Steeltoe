#!/bin/bash

echo Code is built in Unit Tests


cd src/Steeltoe.Common
dotnet restore --configfile ../../nuget.config
cd ../..

cd src/Steeltoe.Common.Http
dotnet restore --configfile ../../nuget.config
cd ../..

cd src/Steeltoe.Common.Autofac
dotnet restore --configfile ../../nuget.config
cd ../..
