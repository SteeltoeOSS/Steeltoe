#!/bin/bash

echo Code is built in Unit Tests

cd src/Steeltoe.Extensions.Logging.DynamicLogger
dotnet restore --configfile ../../nuget.config
cd ../..




