#!/bin/bash
if [[ "$TRAVIS_OS_NAME" == "osx" ]]; then brew update ; fi
if [[ "$TRAVIS_OS_NAME" == "osx" ]]; then brew install openssl ; fi
if [[ "$TRAVIS_OS_NAME" == "osx" ]]; then ln -s /usr/local/opt/openssl/lib/libcrypto.1.0.0.dylib /usr/local/lib/ ; fi
if [[ "$TRAVIS_OS_NAME" == "osx" ]]; then ln -s /usr/local/opt/openssl/lib/libssl.1.0.0.dylib /usr/local/lib/ ; fi
if [[ "$TRAVIS_OS_NAME" == "osx" ]]; then export DOTNET_SDK_URL=https://go.microsoft.com/fwlink/?LinkID=834982 ; fi
if [[ "$TRAVIS_OS_NAME" == "linux" ]]; then export DOTNET_SDK_URL=https://go.microsoft.com/fwlink/?LinkID=834989 ; fi   
export DOTNET_INSTALL_DIR="$PWD/.dotnetsdk"
mkdir -p "$DOTNET_INSTALL_DIR"
curl -L "$DOTNET_SDK_URL" | tar -xzv -C "$DOTNET_INSTALL_DIR"
export PATH="$DOTNET_INSTALL_DIR:$PATH"
dotnet --info
npm install jsonfile -g
