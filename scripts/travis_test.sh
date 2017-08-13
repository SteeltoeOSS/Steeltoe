#!/bin/bash

# Run unit tests
cd test/Steeltoe.Management.Endpoint.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose -framework netcoreapp1.1
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..
cd test/Steeltoe.Management.Endpoint.CloudFoundry.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose -framework netcoreapp1.1
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..
cd test/Steeltoe.Management.Endpoint.Health.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose -framework netcoreapp1.1
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..
cd test/Steeltoe.Management.Endpoint.Info.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose -framework netcoreapp1.1
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..
cd test/Steeltoe.Management.Endpoint.Loggers.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose -framework netcoreapp1.1
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..
cd test/Steeltoe.Management.Endpoint.Trace.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose -framework netcoreapp1.1
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..
cd test/Steeltoe.Management.CloudFoundry.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose -framework netcoreapp1.1
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..