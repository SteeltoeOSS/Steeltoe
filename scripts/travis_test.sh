#!/bin/bash

# Run unit tests 
cd test/Steeltoe.Security.Authentication.CloudFoundry.Test
dotnet restore --configfile ../../nuget.config
dotnet test --framework netcoreapp1.1
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..

cd test/Steeltoe.Security.DataProtection.Redis.Test
dotnet restore --configfile ../../nuget.config
dotnet test --framework netcoreapp1.1
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..