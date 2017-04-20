#!/bin/bash

# Run unit tests 
cd test/Steeltoe.CloudFoundry.Connector.Test
dotnet test --framework netcoreapp1.1
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..

cd test/Steeltoe.CloudFoundry.Connector.PostgreSql.Test
dotnet test --framework netcoreapp1.1
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..

cd test/Steeltoe.CloudFoundry.Connector.Rabbit.Test
dotnet test --framework netcoreapp1.1
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..

cd test/Steeltoe.CloudFoundry.Connector.OAuth.Test
dotnet test --framework netcoreapp1.1
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..

cd test/Steeltoe.CloudFoundry.Connector.MySql.Test
dotnet test --framework netcoreapp1.1
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..

cd test/Steeltoe.CloudFoundry.Connector.Redis.Test
dotnet test --framework netcoreapp1.1
if [[ $? != 0 ]]; then exit 1 ; fi
cd ../..
