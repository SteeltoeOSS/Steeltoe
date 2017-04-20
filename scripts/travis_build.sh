#!/bin/bash

export STEELTOE_VERSION_SUFFIX=$TRAVIS_BRANCH-$TRAVIS_BUILD_NUMBER
echo $STEELTOE_VERSION_SUFFIX

# Restore packages
dotnet restore --configfile nuget.config

cd src/Steeltoe.CloudFoundry.Connector
if [[ "$TRAVIS_TAG" != "" ]]; then dotnet build --framework netstandard1.5 --configuration Release ; fi
if [[ "$TRAVIS_TAG" == "" ]]; then dotnet build --framework netstandard1.5 --configuration Release --version-suffix $STEELTOE_VERSION_SUFFIX ; fi
cd ../..

cd src/Steeltoe.CloudFoundry.Connector.PostgreSql
if [[ "$TRAVIS_TAG" != "" ]]; then dotnet build --framework netstandard1.5 --configuration Release ; fi
if [[ "$TRAVIS_TAG" == "" ]]; then dotnet build --framework netstandard1.5 --configuration Release --version-suffix $STEELTOE_VERSION_SUFFIX ; fi
cd ../..

cd src/Steeltoe.CloudFoundry.Connector.Rabbit
if [[ "$TRAVIS_TAG" != "" ]]; then dotnet build --framework netstandard1.5 --configuration Release ; fi
if [[ "$TRAVIS_TAG" == "" ]]; then dotnet build --framework netstandard1.5 --configuration Release --version-suffix $STEELTOE_VERSION_SUFFIX ; fi
cd ../..

cd src/Steeltoe.CloudFoundry.Connector.OAuth
if [[ "$TRAVIS_TAG" != "" ]]; then dotnet build --framework netstandard1.5 --configuration Release ; fi
if [[ "$TRAVIS_TAG" == "" ]]; then dotnet build --framework netstandard1.5 --configuration Release --version-suffix $STEELTOE_VERSION_SUFFIX ; fi
cd ../..

cd src/Steeltoe.CloudFoundry.Connector.MySql
if [[ "$TRAVIS_TAG" != "" ]]; then dotnet build --framework netstandard1.5 --configuration Release ; fi
if [[ "$TRAVIS_TAG" == "" ]]; then dotnet build --framework netstandard1.5 --configuration Release --version-suffix $STEELTOE_VERSION_SUFFIX ; fi
cd ../..

cd src/Steeltoe.CloudFoundry.Connector.Redis
if [[ "$TRAVIS_TAG" != "" ]]; then dotnet build --framework netstandard1.5 --configuration Release ; fi
if [[ "$TRAVIS_TAG" == "" ]]; then dotnet build --framework netstandard1.5 --configuration Release --version-suffix $STEELTOE_VERSION_SUFFIX ; fi
cd ../..
