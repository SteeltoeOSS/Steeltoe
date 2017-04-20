#!/bin/bash

export STEELTOE_VERSION_SUFFIX=$TRAVIS_BRANCH-$TRAVIS_BUILD_NUMBER
echo $STEELTOE_VERSION_SUFFIX


cd src/Steeltoe.CloudFoundry.Connector
dotnet restore --configfile ../../nuget.config
if [[ "$TRAVIS_TAG" != "" ]]; then dotnet build --framework netstandard1.5 --configuration Release ; fi
if [[ "$TRAVIS_TAG" == "" ]]; then dotnet build --framework netstandard1.5 --configuration Release --version-suffix $STEELTOE_VERSION_SUFFIX ; fi
cd ../..

cd src/Steeltoe.CloudFoundry.Connector.PostgreSql
dotnet restore --configfile ../../nuget.config
if [[ "$TRAVIS_TAG" != "" ]]; then dotnet build --framework netstandard1.5 --configuration Release ; fi
if [[ "$TRAVIS_TAG" == "" ]]; then dotnet build --framework netstandard1.5 --configuration Release --version-suffix $STEELTOE_VERSION_SUFFIX ; fi
cd ../..

cd src/Steeltoe.CloudFoundry.Connector.Rabbit
dotnet restore --configfile ../../nuget.config
if [[ "$TRAVIS_TAG" != "" ]]; then dotnet build --framework netstandard1.5 --configuration Release ; fi
if [[ "$TRAVIS_TAG" == "" ]]; then dotnet build --framework netstandard1.5 --configuration Release --version-suffix $STEELTOE_VERSION_SUFFIX ; fi
cd ../..

cd src/Steeltoe.CloudFoundry.Connector.OAuth
dotnet restore --configfile ../../nuget.config
if [[ "$TRAVIS_TAG" != "" ]]; then dotnet build --framework netstandard1.5 --configuration Release ; fi
if [[ "$TRAVIS_TAG" == "" ]]; then dotnet build --framework netstandard1.5 --configuration Release --version-suffix $STEELTOE_VERSION_SUFFIX ; fi
cd ../..

cd src/Steeltoe.CloudFoundry.Connector.MySql
dotnet restore --configfile ../../nuget.config
if [[ "$TRAVIS_TAG" != "" ]]; then dotnet build --framework netstandard1.5 --configuration Release ; fi
if [[ "$TRAVIS_TAG" == "" ]]; then dotnet build --framework netstandard1.5 --configuration Release --version-suffix $STEELTOE_VERSION_SUFFIX ; fi
cd ../..

cd src/Steeltoe.CloudFoundry.Connector.Redis
dotnet restore --configfile ../../nuget.config
if [[ "$TRAVIS_TAG" != "" ]]; then dotnet build --framework netstandard1.5 --configuration Release ; fi
if [[ "$TRAVIS_TAG" == "" ]]; then dotnet build --framework netstandard1.5 --configuration Release --version-suffix $STEELTOE_VERSION_SUFFIX ; fi
cd ../..
