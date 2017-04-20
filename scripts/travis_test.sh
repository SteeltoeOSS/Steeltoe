#!/bin/bash

# Run unit tests 
cd test/Steeltoe.Discovery.Client.Test
dotnet restore --configfile ../../nuget.config
dotnet test --framework netcoreapp1.1
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..

cd test/Steeltoe.Discovery.Eureka.Client.Test
dotnet restore --configfile ../../nuget.config
dotnet test --framework netcoreapp1.1
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..
