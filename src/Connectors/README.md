# .NET CloudFoundry Connectors

This repository contains connectors for simplifying the process of connecting to backing services and setting up connection health monitoring.

For more information on how to use these components see the [Steeltoe documentation](https://steeltoe.io/).

## Special Build Instructions for GemFire

The driver for Geode/GemFire/Pivotal Cloud Cache is not avaible on nuget.org, so you will need to acquire it directly if you wish to run the GemFire Connector tests.
A cross-platform [powershell script](./EnableGemFire.ps1) is provided inside this folder that is capable of downloading the latest version of the driver from https://network.pivotal.io with the assistance of the [PivNet CLI](https://github.com/pivotal-cf/pivnet-cli). 
The script will also download the PivNet CLI if it is not detected.

Due to the extra steps required to build GemFire dependent projects, the project `GemFireConnector.Test` is configured NOT to build when the solution is configured for `Debug`.
You will also need to build for `Release` or change the project build configurations in order to build the test project.


## Sample Applications

See the `Connectors` directory inside the [Samples](https://github.com/SteeltoeOSS/Samples) repository for examples of how to use these packages.
