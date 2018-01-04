#!/bin/bash

# Run unit tests 
cd test/Steeltoe.Security.Authentication.CloudFoundryCore.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose -f netcoreapp2.0
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..

cd test/Steeltoe.Security.DataProtection.RedisCore.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose  -f netcoreapp2.0
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..

cd test/Steeltoe.Security.DataProtection.CredHubBase.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose  -f netcoreapp2.0
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..
cd test/Steeltoe.Security.DataProtection.CredHubCore.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose  -f netcoreapp2.0
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..