#!/bin/bash
export DOTNET_INSTALL_DIR="$PWD/.dotnetsdk"
export PATH="$DOTNET_INSTALL_DIR:$PATH"

# Run unit tests 
cd test/Steeltoe.Discovery.Client.Test
dotnet test --framework netcoreapp1.1
cd ../..
cd test/Steeltoe.Discovery.Eureka.Client.Test
dotnet test --framework netcoreapp1.1
cd ../..
