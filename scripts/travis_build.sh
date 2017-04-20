#!/bin/bash

export STEELTOE_VERSION_SUFFIX=$TRAVIS_BRANCH-$TRAVIS_BUILD_NUMBER
echo $STEELTOE_VERSION_SUFFIX

cd src/Steeltoe.Discovery.Eureka.Client
dotnet restore --configfile ../../nuget.config
if [[ "$TRAVIS_TAG" != "" ]]; then dotnet build --framework netstandard1.3 --configuration Release ; fi
if [[ "$TRAVIS_TAG" == "" ]]; then dotnet build --framework netstandard1.3 --configuration Release --version-suffix $STEELTOE_VERSION_SUFFIX ; fi
cd ../..

cd src/Steeltoe.Discovery.Client
dotnet restore --configfile ../../nuget.config
if [[ "$TRAVIS_TAG" != "" ]]; then dotnet build --framework netstandard1.3 --configuration Release ; fi
if [[ "$TRAVIS_TAG" == "" ]]; then dotnet build --framework netstandard1.3 --configuration Release --version-suffix $STEELTOE_VERSION_SUFFIX ; fi
cd ../..

