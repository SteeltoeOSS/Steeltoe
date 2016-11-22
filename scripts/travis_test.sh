#!/bin/bash
export DOTNET_INSTALL_DIR="$PWD/.dotnetsdk"
export PATH="$DOTNET_INSTALL_DIR:$PATH"
# Run unit tests 
cd test/Steeltoe.CloudFoundry.Connector.Test
dotnet test --framework netcoreapp1.1
cd ../..
cd test/Steeltoe.CloudFoundry.Connector.PostgreSql.Test
dotnet test --framework netcoreapp1.1
cd ../..
cd test/Steeltoe.CloudFoundry.Connector.Rabbit.Test
dotnet test --framework netcoreapp1.1
cd ../..
cd test/Steeltoe.CloudFoundry.Connector.OAuth.Test
dotnet test --framework netcoreapp1.1
cd ../..
cd test/Steeltoe.CloudFoundry.Connector.MySql.Test
dotnet test --framework netcoreapp1.1
cd ../..
cd test/Steeltoe.CloudFoundry.Connector.Redis.Test
dotnet test --framework netcoreapp1.1
cd ../..
