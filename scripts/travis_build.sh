#!/bin/bash

echo Code is built in Unit Tests

cd src/Steeltoe.CircuitBreaker.Hystrix.Core
dotnet restore --configfile ../../nuget.config
cd ../..

cd src/Steeltoe.CircuitBreaker.HystrixCore
dotnet restore --configfile ../../nuget.config
cd ../..

cd src/Steeltoe.CircuitBreaker.Hystrix.MetricsEventsCore
dotnet restore --configfile ../../nuget.config
cd ../..

cd src/Steeltoe.CircuitBreaker.Hystrix.MetricsStreamCore
dotnet restore --configfile ../../nuget.config
cd ../..

