#!/bin/bash
export STEELTOE_VERSION="1.0.0"
export DOTNET_INSTALL_DIR="$PWD/.dotnetsdk"
export PATH="$DOTNET_INSTALL_DIR:$PATH"
#  Patch project.json files
cd ./scripts
npm install
node patch-project-json.js ../src/SteelToe.Extensions.Configuration.CloudFoundry/project.json $STEELTOE_VERSION-$TRAVIS_BRANCH-$TRAVIS_BUILD_NUMBER $TRAVIS_TAG
node patch-project-json.js ../src/SteelToe.Extensions.Configuration.ConfigServer/project.json $STEELTOE_VERSION-$TRAVIS_BRANCH-$TRAVIS_BUILD_NUMBER $TRAVIS_TAG
cd ..
cd src
dotnet restore
cd ../test
dotnet restore
cd ..
cd src/SteelToe.Extensions.Configuration.CloudFoundry
dotnet build --framework netstandard1.3 --configuration Release
cd ../..
cd src/SteelToe.Extensions.Configuration.ConfigServer
dotnet build --framework netstandard1.3 --configuration Release
cd ../..
