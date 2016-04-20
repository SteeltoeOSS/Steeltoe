#!/bin/bash
source $HOME/.dnx/dnvm/dnvm.sh

# Target (x64 CoreCLR)
dnvm use 1.0.0-rc1-update1 -a x64 -r coreclr

# Run unit tests (x64 CoreCLR)
cd test/SteelToe.Extensions.Configuration.CloudFoundry.Test
dnx test
cd ../..
cd test/SteelToe.Extensions.Configuration.ConfigServer.Test
dnx test
cd ../..

