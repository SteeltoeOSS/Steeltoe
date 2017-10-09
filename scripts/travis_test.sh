#!/bin/bash

# Run unit tests 
cd test/Steeltoe.Extensions.Configuration.CloudFoundry.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose -framework netcoreapp2.0
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..

cd test/Steeltoe.Extensions.Configuration.CloudFoundryCore.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose -framework netcoreapp2.0
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..

cd test/Steeltoe.Extensions.Configuration.CloudFoundryAutofac.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose -framework netcoreapp2.0
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..

cd test/Steeltoe.Extensions.Configuration.ConfigServer.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose -framework netcoreapp2.0
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..

cd test/Steeltoe.Extensions.Configuration.ConfigServerCore.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose -framework netcoreapp2.0
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..

cd test/Steeltoe.Extensions.Configuration.ConfigServerAutofac.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose -framework netcoreapp2.0
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..