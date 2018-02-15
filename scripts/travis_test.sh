#!/bin/bash

# Run unit tests 
cd test/Steeltoe.CloudFoundry.ConnectorBase.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose -framework netcoreapp2.0
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..

cd test/Steeltoe.CloudFoundry.Connector.EFCore.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose -framework netcoreapp2.0
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..

cd test/Steeltoe.CloudFoundry.ConnectorCore.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose -framework netcoreapp2.0
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..
