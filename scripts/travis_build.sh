#!/bin/bash

echo Code is built in Unit Tests

cd src/Steeltoe.Security.Authentication.CloudFoundryCore
dotnet restore --configfile ../../nuget.config
cd ../..

cd src/Steeltoe.Security.DataProtection.RedisCore
dotnet restore --configfile ../../nuget.config
cd ../..

cd src/Steeltoe.Security.DataProtection.CredHub
dotnet restore --configfile ../../nuget.config
cd ../..

cd src/Steeltoe.Security.DataProtection.CredHubCore
dotnet restore --configfile ../../nuget.config
cd ../..

