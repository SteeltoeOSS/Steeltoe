@ECHO OFF
:: Install (x64 CLR)
call dnvm install 1.0.0-rc1-update1 -a x64 -r clr
:: Install & target (x86 CLR)
call dnvm install 1.0.0-rc1-update1 -a x86 -r clr
:: Install & target (x64 CoreCLR)
call dnvm install 1.0.0-rc1-update1 -a x64 -r coreclr
:: Install & target (x86 CoreCLR)
call dnvm install 1.0.0-rc1-update1 -a x86 -r coreclr
:: For patching project.json's
call npm install jsonfile -g
