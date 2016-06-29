#!/bin/bash
export DOTNET_INSTALL_DIR="$PWD/.dotnetsdk"
export PATH="$DOTNET_INSTALL_DIR:$PATH"
cd src
dotnet restore
cd ../test
dotnet restore
cd ..
cd src/SteelToe.CloudFoundry.Connector
dotnet build --framework netstandard1.3 --configuration Release
# cd ../..
# cd src/SteelToe.CloudFoundry.Connector.MySql
# dotnet build --framework netstandard1.3 --configuration Release
# cd ../..
# cd src/SteelToe.CloudFoundry.Connector.Redis
# dotnet build --framework netstandard1.3 --configuration Release
# cd ../..
