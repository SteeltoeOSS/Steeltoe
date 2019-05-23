# .NET Application Management Endpoints

This repository contains several management endpoints which can be used to help monitor and manage your applications.

## .NET Runtime & Framework Support

Like ASP.NET Core, the these endpoint packages are intended to support both .NET 4.6+ and .NET Core (CoreCLR/CoreFX) runtimes.

Where supported the management endpoints are built and unit tested on Windows, Linux and OSX.

Depending on their level of support, the endpoints and samples have been tested on .NET Core 2.0, .NET 4.6.x, and on ASP.NET Core 2.0.0.

## Usage

For more information on how to use these components see the online [Steeltoe documentation](https://steeltoe.io/).

## Nuget Feeds

All new endpoint development is done on the dev branch. More stable versions of the endpoints can be found on the master branch. The latest prebuilt packages from each branch can be found on one of two MyGet feeds. Released version can be found on nuget.org.

- [Development feed (Less Stable)](https://www.myget.org/gallery/steeltoedev)
- [Master feed (Stable)](https://www.myget.org/gallery/steeltoemaster)
- [Release or Release Candidate feed](https://www.nuget.org/)

## Building Pre-requisites

To build and run the unit tests:

1. .NET Core SDK 2.0.3+
1. .NET Core Runtime 2.0.3

## Building Packages & Running Tests - Windows

To build the packages on windows:

1. git clone ...
1. cd `<clone directory>`
1. cd src/`<project>` (e.g. cd src/Steeltoe.Management.EndpointBase)
1. dotnet restore
1. dotnet pack --configuration `<Release or Debug>`

The resulting artifacts can be found in the bin folder under the corresponding project. (e.g. src/Steeltoe.Management.EndpointBase/bin)

To run the unit tests:

1. git clone ...
1. cd `<clone directory>`
1. cd test/`<test project>` (e.g. cd test/Steeltoe.Management.EndpointBase.Test)
1. dotnet restore
1. dotnet xunit -verbose

## Building Packages & Running Tests - Linux/OSX

To build the packages on Linux/OSX:

1. git clone ...
1. cd `<clone directory>`
1. cd src/`<project>` (e.g.. cd src/Steeltoe.Management.EndpointBase)
1. dotnet restore
1. dotnet pack --configuration `<Release or Debug>`

The resulting artifacts can be found in the bin folder under the corresponding project. (e.g. src/Steeltoe.Management.EndpointBase/bin

To run the unit tests:

1. git clone ...
1. cd `<clone directory>`
1. cd test/`<test project>` (e.g. cd test/Steeltoe.Management.EndpointBase.Test)
1. dotnet restore
1. dotnet xunit -verbose -framework netcoreapp2.0

## Sample Applications

See the [Samples](https://github.com/SteeltoeOSS/Samples) repository for examples of how to use these packages.
