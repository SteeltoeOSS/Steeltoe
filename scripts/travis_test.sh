#!/bin/bash

# Run unit tests 
cd test/Steeltoe.Extensions.Configuration.CloudFoundry.Test
dotnet test --framework netcoreapp1.1
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..
cd test/Steeltoe.Extensions.Configuration.ConfigServer.Test
dotnet test --framework netcoreapp1.1
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..

