#!/bin/bash

# Run unit tests
cd test/Steeltoe.Extensions.Logging.CloudFoundry.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose -framework netcoreapp2.0
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..
