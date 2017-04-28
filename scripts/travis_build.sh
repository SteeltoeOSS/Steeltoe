#!/bin/bash

echo Code is built in Unit Tests

cd src/Steeltoe.Security.Authentication.CloudFoundry
dotnet restore --configfile ../../nuget.config
cd ../..

cd src/Steeltoe.Security.DataProtection.Redis
dotnet restore --configfile ../../nuget.config
cd ../..

