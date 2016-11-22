#!/bin/bash
export STEELTOE_VERSION="1.0.0"
export DOTNET_INSTALL_DIR="$PWD/.dotnetsdk"
export PATH="$DOTNET_INSTALL_DIR:$PATH"
# Patch project.json files
cd ./scripts
npm install
node patch-project-json.js ../src/Steeltoe.CloudFoundry.Connector/project.json $STEELTOE_VERSION-$TRAVIS_BRANCH-$TRAVIS_BUILD_NUMBER $TRAVIS_TAG
node patch-project-json.js ../src/Steeltoe.CloudFoundry.Connector.PostgreSql/project.json $STEELTOE_VERSION-$TRAVIS_BRANCH-$TRAVIS_BUILD_NUMBER $TRAVIS_TAG
node patch-project-json.js ../src/Steeltoe.CloudFoundry.Connector.Rabbit/project.json $STEELTOE_VERSION-$TRAVIS_BRANCH-$TRAVIS_BUILD_NUMBER $TRAVIS_TAG
node patch-project-json.js ../src/Steeltoe.CloudFoundry.Connector.OAuth/project.json $STEELTOE_VERSION-$TRAVIS_BRANCH-$TRAVIS_BUILD_NUMBER $TRAVIS_TAG
node patch-project-json.js ../src/Steeltoe.CloudFoundry.Connector.MySql/project.json $STEELTOE_VERSION-$TRAVIS_BRANCH-$TRAVIS_BUILD_NUMBER $TRAVIS_TAG
node patch-project-json.js ../src/Steeltoe.CloudFoundry.Connector.Redis/project.json $STEELTOE_VERSION-$TRAVIS_BRANCH-$TRAVIS_BUILD_NUMBER $TRAVIS_TAG
cd ..
cd src
dotnet restore
cd ../test
dotnet restore
cd ..
cd src/Steeltoe.CloudFoundry.Connector
dotnet build --framework netstandard1.3 --configuration Release
cd ../..
cd src/Steeltoe.CloudFoundry.Connector.PostgreSql
dotnet build --framework netstandard1.3 --configuration Release

cd ../..
cd src/Steeltoe.CloudFoundry.Connector.Rabbit
dotnet build --framework netstandard1.3 --configuration Release

cd ../..
cd src/Steeltoe.CloudFoundry.Connector.OAuth
dotnet build --framework netstandard1.3 --configuration Release

cd ../..
cd src/Steeltoe.CloudFoundry.Connector.MySql
dotnet build --framework netstandard1.6 --configuration Release

cd ../..
cd src/Steeltoe.CloudFoundry.Connector.Redis
dotnet build --framework netstandard1.5 --configuration Release
cd ../..
