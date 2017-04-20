#!/bin/bash

# Restore packages
dotnet restore --configfile nuget.config

# Build code
cd src/Steeltoe.Extensions.Configuration.CloudFoundry
if [[ "$TRAVIS_TAG" != "" ]]; then dotnet build --framework netstandard1.3 --configuration Release ; fi
if [[ "$TRAVIS_TAG" == "" ]]; then dotnet build --framework netstandard1.3 --configuration Release --version-suffix $STEELTOE_VERSION_SUFFIX ; fi
cd ../..
cd src/Steeltoe.Extensions.Configuration.ConfigServer
if [[ "$TRAVIS_TAG" != "" ]]; then dotnet build --framework netstandard1.3 --configuration Release ; fi
if [[ "$TRAVIS_TAG" == "" ]]; then dotnet build --framework netstandard1.3 --configuration Release --version-suffix $STEELTOE_VERSION_SUFFIX ; fi
cd ../..
