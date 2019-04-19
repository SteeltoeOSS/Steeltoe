# .NET CloudFoundry Connectors

This repository contains several connectors which simplify the process of connecting to services on CloudFoundry.

Windows Master (Stable): [![AppVeyor Master](https://ci.appveyor.com/api/projects/status/ivdciaopp5kxo3cp/branch/master?svg=true)](https://ci.appveyor.com/project/steeltoe/connectors/branch/master)

Windows Dev (Less Stable): [![AppVeyor Dev](https://ci.appveyor.com/api/projects/status/ivdciaopp5kxo3cp/branch/dev?svg=true)](https://ci.appveyor.com/project/steeltoe/connectors/branch/dev)

Linux/OS X Master (Stable): [![Travis Master](https://travis-ci.org/SteeltoeOSS/Connectors.svg?branch=master)](https://travis-ci.org/SteeltoeOSS/Connectors)

Linux/OS X Dev (Less Stable):  [![Travis Dev](https://travis-ci.org/SteeltoeOSS/Connectors.svg?branch=dev)](https://travis-ci.org/SteeltoeOSS/Connectors)

## .NET Runtime & Framework Support

The connectors are intended to support both .NET 4.6.1+ and .NET Core (CoreCLR/CoreFX) runtimes. Note that some connectors only support .NET 4.6.1+ since the libraries they depend on do not support .NET Core.

Where supported the connectors are built and unit tested on Windows, Linux and OSX.

While the primary usage of the connectors is intended to be with ASP.NET Core applications, they should also work fine with UWP, Console and ASP.NET 4.x apps.

Depending on their level of support, the connectors and samples have been tested  on .NET Core 2.0, .NET 4.6.x, and on ASP.NET Core 2.0.

## Usage

For more information on how to use these components see the online [Steeltoe documentation](https://steeltoe.io/).

## Nuget Feeds

All new connector development is done on the dev branch. More stable versions of the connectors can be found on the master branch. The latest prebuilt packages from each branch can be found on one of two MyGet feeds. Released version can be found on nuget.org.

- [Development feed (Less Stable)](https://www.myget.org/gallery/steeltoedev)
- [Master feed (Stable)](https://www.myget.org/gallery/steeltoemaster)
- [Release or Release Candidate feed](https://www.nuget.org/)

## Building Pre-requisites

To build and run the unit tests:

1. .NET Core SDK 2.0.3 or greater
1. .NET Core Runtime 2.0.3

## Building Packages & Running Tests - Windows

To build the packages on windows:

1. git clone ...
1. cd `<clone directory>`
1. cd src/`<project>` (e.g. cd src/Steeltoe.CloudFoundry.Connector)
1. dotnet restore
1. dotnet pack --configuration `<Release or Debug>`

The resulting artifacts can be found in the bin folder under the corresponding project. (e.g. src/Steeltoe.CloudFoundry.Connector\bin)

To run the unit tests:

1. git clone ...
1. cd `<clone directory>`
1. cd test/`<test project>` (e.g. cd test/Steeltoe.CloudFoundry.Connector.Test)
1. dotnet restore
1. dotnet xunit -verbose

## Building Packages & Running Tests - Linux/OSX

To build the packages on Linux/OSX: ( Note: Some connectors do not support CoreCLR.)

1. git clone ...
1. cd `<clone directory>`
1. cd src/`<project>` (e.g.. cd src/Steeltoe.CloudFoundry.Connector)
1. dotnet restore
1. dotnet pack --configuration `<Release or Debug>`

The resulting artifacts can be found in the bin folder under the corresponding project. (e.g. src/Steeltoe.CloudFoundry.Connector/bin

To run the unit tests: ( Note: Some connectors do not support CoreCLR.)

1. git clone ...
1. cd `<clone directory>`
1. cd test/`<test project>` (e.g. cd test/Steeltoe.CloudFoundry.Connector.Test)
1. dotnet restore
1. dotnet xunit -verbose -framework netcoreapp2.0

## Sample Applications

See the [Samples](https://github.com/SteeltoeOSS/Samples) repository for examples of how to use these packages.
