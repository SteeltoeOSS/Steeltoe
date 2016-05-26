#!/bin/bash
cd src
dotnet restore
cd ../test
dotnet restore
cd ..
cd src/SteelToe.Extensions.Configuration.CloudFoundry
dotnet build --framework netstandard1.3 --configuration Release
cd ../..
cd src/SteelToe.Extensions.Configuration.ConfigServer
dotnet build --framework netstandard1.3 --configuration Release
cd ../..
