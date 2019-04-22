# .NET Logging Extensions
This repo contains a Logging extension that when used with the Steeltoe Management Logger Endpoint enables changing the Logging levels for a running application dynamically using the Pivotal Apps manager console.

This logger is simply a wrapper around the Microsoft Console logger, but enables querying and dynamically changing the logging levels of all the currently active loggers.


Windows Master (Stable):  [![AppVeyor Master](https://ci.appveyor.com/api/projects/status/oj7275o04e7u2jk3/branch/master?svg=true
)](https://ci.appveyor.com/project/steeltoe/Logging)

Windows Dev (Less Stable):  [![AppVeyor Dev](https://ci.appveyor.com/api/projects/status/oj7275o04e7u2jk3/branch/dev?svg=true
)](https://ci.appveyor.com/project/steeltoe/logging)

Linux/OS X Master (Stable): [![Travis Master](https://travis-ci.org/SteeltoeOSS/Logging.svg?branch=master)](https://travis-ci.org/SteeltoeOSS/Logging)

Linux/OS X Dev (Less Stable): [![Travis Dev](https://travis-ci.org/SteeltoeOSS/Logging.svg?branch=dev)](https://travis-ci.org/SteeltoeOSS/Logging)


# .NET Runtime & Framework Support
Like the ASP.NET Core Logging providers, these providers are intended to support both .NET 4.6+ and .NET Core (CoreCLR/CoreFX) run-times.  The providers are built and unit tested on Windows, Linux and OSX.

While the primary usage of the providers is intended to be with ASP.NET Core applications, they should also work fine with UWP, Console and ASP.NET 4.x apps. 

Currently all of the code and samples have been tested on .NET Core 1.1, .NET 4.6.x, and on ASP.NET Core 1.1.0.

# Usage
See the [Steeltoe documentation](http://steeltoe.io/) for information on how to use these components in your applications.

# Nuget Feeds
All new configuration provider development is done on the dev branch. More stable versions of the providers can be found on the master branch. The latest prebuilt packages from each branch can be found on one of two MyGet feeds. Released version can be found on nuget.org.

[Development feed (Less Stable)](https://www.myget.org/gallery/steeltoedev) - https://www.myget.org/gallery/steeltoedev

[Master feed (Stable)](https://www.myget.org/gallery/steeltoemaster) - https://www.myget.org/gallery/steeltoemaster

[Release or Release Candidate feed](https://www.nuget.org/) - https://www.nuget.org/. 

# Building Pre-requisites
To build and run the unit tests:

1. .NET Core SDK 

# Building Packages & Running Tests - Windows
To build the packages on windows:

1. git clone ...
2. cd clone directory
3. Install .NET Core SDK
4. dotnet restore src
5. cd src\<project> (e.g. cd src\Steeltoe.Extensions.Logging.CloudFoundry)
6. dotnet pack --configuration Release or Debug

The resulting artifacts can be found in the bin folder under the corresponding project. (e.g. src\Steeltoe.Extensions.Logging.CloudFoundry\bin

To run the unit tests:

1. git clone ...
2. cd clone directory
3. Install .NET Core SDK 
4. dotnet restore test
5. cd test\<test project> (e.g. cd test\Steeltoe.Extensions.Logging.CloudFoundry.Test)
6. dotnet xunit -verbose 

# Building Packages & Running Tests - Linux/OSX
To build the packages on Linux/OSX:

1. git clone ...
2. cd clone directory
3. Install .NET Core SDK
4. dotnet restore src
5. cd src/<project> (e.g.. cd src/Steeltoe.Extensions.Logging.CloudFoundry)
6. dotnet pack --configuration Release or Debug

The resulting artifacts can be found in the bin folder under the corresponding project. (e.g. src/Steeltoe.Extensions.Logging.CloudFoundry/bin

To run the unit tests:

1. git clone ...
2. cd clone directory
3. Install .NET Core SDK 
4. dotnet restore test
5. cd test\<test project> (e.g. cd test/Steeltoe.Extensions.Logging.CloudFoundry.Test)
6. dotnet xunit -verbose -framework netcoreapp2.0

# Sample Applications
See the [Samples](https://github.com/SteeltoeOSS/Samples) repo for examples of how to use these packages.