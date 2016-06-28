#!/bin/bash
if [[ "$TRAVIS_OS_NAME" == "osx" ]]; then export DOTNET_SDK_URL=https://go.microsoft.com/fwlink/?LinkID=798404 ; fi
if [[ "$TRAVIS_OS_NAME" == "linux" ]]; then export DOTNET_SDK_URL=https://go.microsoft.com/fwlink/?LinkID=798405 ; fi   
export DOTNET_INSTALL_DIR="$PWD/.dotnetsdk"
mkdir -p "$DOTNET_INSTALL_DIR"
curl -L "$DOTNET_SDK_URL" | tar -xzv -C "$DOTNET_INSTALL_DIR"
export PATH="$DOTNET_INSTALL_DIR:$PATH"
dotnet --info
