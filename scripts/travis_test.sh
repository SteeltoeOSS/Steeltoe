#!/bin/bash

# Run unit tests 
cd test/SteelToe.Extensions.Configuration.CloudFoundry.Test
dotnet test
cd ../..
cd test/SteelToe.Extensions.Configuration.ConfigServer.Test
dotnet test
cd ../..

