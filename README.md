# .NET CloudFoundry Connectors

This repo contains several connectors which simplify the process of connecting to services on CloudFoundry.  

Windows Master (Stable):  [![AppVeyor Master](https://ci.appveyor.com/api/projects/status/27c2hd0460aac1cs/branch/master?svg=true)](https://ci.appveyor.com/project/steeltoe/Configuration)

Windows Dev (Less Stable):  [![AppVeyor Dev](https://ci.appveyor.com/api/projects/status/27c2hd0460aac1cs/branch/dev?svg=true)](https://ci.appveyor.com/project/steeltoe/Configuration)

Linux/OS X Master (Stable): [![Travis Master](https://travis-ci.org/SteelToeOSS/Configuration.svg?branch=master)](https://travis-ci.org/SteelToeOSS/Configuration)

Linux/OS X Dev (Less Stable): [![Travis Dev](https://travis-ci.org/SteelToeOSS/Configuration.svg?branch=dev)](https://travis-ci.org/SteelToeOSS/Configuration)

# .NET Runtime & Framework Support
Like ASP.NET Core, the connectors are intended to support both .NET 4.5.1+ and .NET Core (CoreCLR/CoreFX) runtimes.  Where supported the connectors are built and unit tested on Windows, Linux and OSX.

Note that some connectors do not support running on .NET Core (CoreCLR/CoreFX).

While the primary usage of the connectors is intended to be with ASP.NET Core applications, they should also work fine with UWP, Console and ASP.NET 4.x apps.

Depending on their level of support, the connectors and samples have been tested on .NET Core 1.0.0-RC2/SDK Preview 1, .NET 4.5.1, and on ASP.NET Core 1.0.0-RC2-final.

# Usage
See the Readme for each connector for more details on how to make use of it in an application.

# Nuget Feeds
All new configuration connector development is done on the dev branch. More stable versions of the connectors can be found on the master branch. The latest prebuilt packages from each branch can be found on one of two MyGet feeds. Released version can be found on nuget.org.

[Development feed (Less Stable)](https://www.myget.org/gallery/steeltoedev) - https://www.myget.org/gallery/steeltoedev

[Master feed (Stable)](https://www.myget.org/gallery/steeltoemaster) - https://www.myget.org/gallery/steeltoemaster

[Release or Release Candidate feed](https://www.nuget.org/) - https://www.nuget.org/. 

# Building Packages & Running Tests - Windows
To build the packages on windows:

1. git clone ...
2. cd <clone directory>
3. Install .NET Core SDK
4. dotnet restore src
5. cd src\<project> (e.g. cd src\SteelToe.CloudFoundry.Connector)
6. dotnet pack --configuration <Release or Debug> 

The resulting artifacts can be found in the bin folder under the corresponding project. (e.g. src\SteelToe.CloudFoundry.Connector\bin

To run the unit tests:

1. git clone ...
2. cd <clone directory>
3. Install .NET Core SDK 
4. dotnet restore test
5. cd test\<test project> (e.g. cd test\SteelToe.CloudFoundry.Connector.Test)
6. dotnet test

# Building Packages & Running Tests - Linux/OSX
To build the packages on Linux/OSX: ( Note: Some connectors do not support CoreCLR.)

1. git clone ...
2. cd <clone directory>
3. Install .NET Core SDK
4. dotnet restore src
5. cd src/<project> (e.g.. cd src/SteelToe.CloudFoundry.Connector)
6. dotnet pack --configuration <Release or Debug> 

The resulting artifacts can be found in the bin folder under the corresponding project. (e.g. src/SteelToe.CloudFoundry.Connector/bin

To run the unit tests: ( Note: Some connectors do not support CoreCLR.)

1. git clone ...
2. cd <clone directory>
3. Install .NET Core SDK 
4. dotnet restore test
5. cd test\<test project> (e.g. cd test/SteelToe.CloudFoundry.Connector.Test)
6. dotnet test --framework netcoreapp1.0

# Sample Applications
See the [Samples](https://github.com/SteelToeOSS/Samples) repo for examples of how to use these packages.