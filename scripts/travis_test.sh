#!/bin/bash

# Run unit tests
cd test/Steeltoe.Discovery.Eureka.Client.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose -framework netcoreapp2.0
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..

cd test/Steeltoe.Discovery.ClientCore.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose -framework netcoreapp2.0
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..
