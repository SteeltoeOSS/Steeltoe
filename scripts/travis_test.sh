#!/bin/bash
export DOTNET_INSTALL_DIR="$PWD/.dotnetsdk"
export PATH="$DOTNET_INSTALL_DIR:$PATH"
# Run unit tests 
cd test/Steeltoe.Security.Authentication.CloudFoundry.Test
dotnet test --framework netcoreapp1.1
cd ../..
cd test/Steeltoe.Security.DataProtection.Redis.Test
dotnet test --framework netcoreapp1.1
cd ../..