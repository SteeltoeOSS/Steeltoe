#!/bin/bash
export DOTNET_INSTALL_DIR="$PWD/.dotnetsdk"
export PATH="$DOTNET_INSTALL_DIR:$PATH"

# Run unit tests 
cd test/SteelToe.Discovery.Client.Test
dotnet test --framework netcoreapp1.0
cd ../..
cd test/SteelToe.Discovery.Eureka.Client.Test
dotnet test --framework netcoreapp1.0
cd ../..
