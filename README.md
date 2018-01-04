# ASP.NET Security Providers

This repository contains various security providers which simplify the process of using Security services on CloudFoundry.

Windows Master (Stable): [![AppVeyor Master](https://ci.appveyor.com/api/projects/status/w27a5c4x64pd7jyq?svg=true)](https://ci.appveyor.com/project/steeltoe/security)

Windows Dev (Less Stable): [![AppVeyor Dev](https://ci.appveyor.com/api/projects/status/w27a5c4x64pd7jyq/branch/dev?svg=true)](https://ci.appveyor.com/project/steeltoe/security/branch/dev)

Linux/OS X Master (Stable): [![Travis Master](https://travis-ci.org/SteeltoeOSS/Security.svg?branch=master)](https://travis-ci.org/SteelToeOSS/Security)

Linux/OS X Dev (Less Stable):  [![Travis Dev](https://travis-ci.org/SteeltoeOSS/Security.svg?branch=dev)](https://travis-ci.org/SteelToeOSS/Security)

## .NET Runtime & Framework Support

The components are intended to support both .NET 4.6.1+ and .NET Core (CoreCLR/CoreFX).

Where appropriate, the components are built and unit tested on Windows, Linux and OSX.

Where appropriate, the components and samples have been tested on .NET Core SDK, .NET 4.6.1+, and on ASP.NET Core 2.0.

## Usage

For more information on how to use these components see the online [Steeltoe documentation](http://steeltoe.io/).

## Nuget Feeds

All new development is done on the dev branch. More stable versions of the components can be found on the master branch. The latest prebuilt packages from each branch can be found on one of two MyGet feeds. Released version can be found on nuget.org.

[Development feed (Less Stable)](https://www.myget.org/gallery/steeltoedev) - https://www.myget.org/gallery/steeltoedev

[Master feed (Stable)](https://www.myget.org/gallery/steeltoemaster) - https://www.myget.org/gallery/steeltoemaster

[Release or Release Candidate feed](https://www.nuget.org/) - https://www.nuget.org/. 

## Building Pre-requisites

To build and run the unit tests:

1. .NET Core SDK 2.0.3 or greater
1. .NET Core Runtime 2.0.3

## Building Packages & Running Tests - Windows

To build the packages on windows:

1. git clone ...
1. cd clone directory
1. cd src/project (e.g. cd src/Steeltoe.Security.Authentication.CloudFoundry)
1. dotnet restore
1. dotnet pack --configuration Release or Debug

The resulting artifacts can be found in the bin folder under the corresponding project. (e.g. src/Steeltoe.Security.Authentication.CloudFoundry/bin)

To run the unit tests:

1. git clone ...
1. cd clone directory
1. cd test/test project (e.g. cd test/Steeltoe.Security.Authentication.CloudFoundry.Test)
1. dotnet restore
1. dotnet xunit -verbose

## Building Packages & Running Tests - Linux/OSX

To build the packages on Linux/OSX:

1. git clone ...
1. cd clone directory
1. cd src/project (e.g.. cd src/Steeltoe.Security.Authentication.CloudFoundry)
1. dotnet pack --configuration Release or Debug

The resulting artifacts can be found in the bin folder under the corresponding project. (e.g. src/Steeltoe.Security.Authentication.CloudFoundry/bin

To run the unit tests:

1. git clone ...
1. cd clone directory
1. cd test/test project (e.g. cd test/Steeltoe.Security.Authentication.CloudFoundry.Test)
1. dotnet xunit -verbose -framework netcoreapp2.0

## Sample Applications

See the [Samples](https://github.com/SteelToeOSS/Samples) repository for examples of how to use these packages.
