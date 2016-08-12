#!/bin/bash
export DOTNET_INSTALL_DIR="$PWD/.dotnetsdk"
export PATH="$DOTNET_INSTALL_DIR:$PATH"
# Run unit tests 
cd test/SteelToe.CloudFoundry.Connector.Test
dotnet test --framework netcoreapp1.0
cd ../..
cd test/SteelToe.CloudFoundry.Connector.PostgreSql.Test
dotnet test --framework netcoreapp1.0
cd ../..
cd test/SteelToe.CloudFoundry.Connector.Rabbit.Test
dotnet test --framework netcoreapp1.0
# cd ../..
# cd test/SteelToe.CloudFoundry.Connector.MySql.Test
# dotnet test --framework netcoreapp1.0
# cd ../..
# cd test/SteelToe.CloudFoundry.Connector.Redis.Test
# dotnet test --framework netcoreapp1.0
# cd ../..
