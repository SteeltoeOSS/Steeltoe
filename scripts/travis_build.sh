#!/bin/bash

echo Code is built in Unit Tests

cd src/Steeltoe.Extensions.Logging.CloudFoundry
dotnet restore --configfile ../../nuget.config
cd ../..




