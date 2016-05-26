#!/bin/bash
export DOTNET_INSTALL_DIR="$PWD/.dotnetsdk"
export PATH="$DOTNET_INSTALL_DIR:$PATH"
# Run unit tests 
cd test/SteelToe.Extensions.Configuration.CloudFoundry.Test
dotnet test --framework netstandard1.3
cd ../..
cd test/SteelToe.Extensions.Configuration.ConfigServer.Test
dotnet test --framework netstandard1.3
cd ../..

