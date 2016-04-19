#!/bin/bash
source /home/travis/.dnx/dnvm/dnvm.sh

# Target (x64 CoreCLR)
dnvm use 1.0.0-rc1-update1 -a x64 -r coreclr

# Run unit tests (x64 CoreCLR)
cd test/SteelToe.Discovery.Client.Test
dnx test
cd ../..
cd test/SteelToe.Discovery.Eureka.Client.Test
dnx test
cd ../..
