#!/bin/bash

echo Code is built in Unit Tests

cd src/Steeltoe.CircuitBreaker.Hystrix.Core
dotnet restore --configfile ../../nuget.config
cd ../..
cd src/Steeltoe.CircuitBreaker.Hystrix
dotnet restore --configfile ../../nuget.config
cd ../..
cd src/Steeltoe.CircuitBreaker.Hystrix.MetricsEvents
dotnet restore --configfile ../../nuget.config
cd ../..
cd src/Steeltoe.CircuitBreaker.Hystrix.MetricsStream
dotnet restore --configfile ../../nuget.config
cd ../..



