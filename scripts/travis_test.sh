#!/bin/bash
export DOTNET_INSTALL_DIR="$PWD/.dotnetsdk"
export PATH="$DOTNET_INSTALL_DIR:$PATH"
# Run unit tests 
cd test/Steeltoe.Extensions.Configuration.CloudFoundry.Test
dotnet test --framework netcoreapp1.1
cd ../..
cd test/Steeltoe.Extensions.Configuration.ConfigServer.Test
dotnet test --framework netcoreapp1.1
cd ../..

