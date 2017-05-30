#!/bin/bash

echo Code is built in Unit Tests

cd src/Steeltoe.CircuitBreaker.Hystrix.Core
dotnet restore --configfile ../../nuget.config
cd ../..




