#!/bin/bash

# Run unit tests
cd test/Steeltoe.CircuitBreaker.Hystrix.Core.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose -framework netcoreapp1.1
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..
cd test/Steeltoe.CircuitBreaker.Hystrix.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose -framework netcoreapp1.1
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..
cd test/Steeltoe.CircuitBreaker.Hystrix.MetricsEvents.Test
dotnet restore --configfile ../../nuget.config
dotnet xunit -verbose -framework netcoreapp1.1
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..
