#!/bin/bash   
curl -sSL https://raw.githubusercontent.com/aspnet/Home/dev/dnvminstall.sh | DNX_BRANCH=dev sh && source ~/.dnx/dnvm/dnvm.sh
dnvm install 1.0.0-rc1-update1 -a x64 -r coreclr
