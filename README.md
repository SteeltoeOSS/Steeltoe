# .NET Configuration Providers

With the introduction of ASP.NET 5, Microsoft is providing a new [application configuration model](http://docs.asp.net/en/latest/fundamentals/configuration.html) for accessing configuration settings for an application. This new model supports access to key/value configuration data from a variety of different configuration providers or sources. Out of the box, ASP.NET 5 comes with support for [JSON](https://github.com/aspnet/Configuration/tree/dev/src/Microsoft.Extensions.Configuration.Json), [XML](https://github.com/aspnet/Configuration/tree/dev/src/Microsoft.Extensions.Configuration.Xml) and [INI](https://github.com/aspnet/Configuration/tree/dev/src/Microsoft.Extensions.Configuration.Ini) files, as well as environment variables and command line parameters.  Additionally, Microsoft has also enabled developers to write their own [custom configuration providers](http://docs.asp.net/en/latest/fundamentals/configuration.html#custom-config-providers) should those provided by Microsoft not meet your needs.  

This repo contains two custom configuration providers.  The [SteelToe.Extensions.Configuration.ConfigServer](https://github.com/SteelToeOSS/Configuration/tree/master/src/SteelToe.Extensions.Configuration.ConfigServer) enables using the [Spring Cloud Config Server](http://projects.spring.io/spring-cloud/) as a provider of configuration data and the [SteelToe.Extensions.Configuration.CloudFoundry](https://github.com/SteelToeOSS/Configuration/tree/master/src/SteelToe.Extensions.Configuration.CloudFoundry) provider enables [CloudFoundry environment variables](docs.cloudfoundry.org) to be parsed and accessed as configuration data.

Windows Master (Stable):  [![AppVeyor Master](https://ci.appveyor.com/api/projects/status/27c2hd0460aac1cs/branch/master?svg=true)](https://ci.appveyor.com/project/steeltoe/Configuration)

Windows Dev (Less Stable):  [![AppVeyor Dev](https://ci.appveyor.com/api/projects/status/27c2hd0460aac1cs/branch/dev?svg=true)](https://ci.appveyor.com/project/steeltoe/Configuration)

Linux/OS X Master (Stable): [![Travis Master](https://travis-ci.org/SteelToeOSS/Configuration.svg?branch=master)](https://travis-ci.org/SteelToeOSS/Configuration)

Linux/OS X Dev (Less Stable): [![Travis Dev](https://travis-ci.org/SteelToeOSS/Configuration.svg?branch=dev)](https://travis-ci.org/SteelToeOSS/Configuration)

# .NET Runtime & Framework Support
Like ASP.NET 5, the providers are intended to support both .NET 4.5.1+ and .NET Core (CoreCLR/CoreFX).  The providers are built and tested on Windows, Linux and OSX.

While the primary usage of the providers is intended to be with ASP.NET 5 applications, they should also work fine with UWP, console and ASP.NET 4.x apps. An ASP.NET 4.x sample app is available illustrating how this can be done.

Currently they have been tested on DNX 1.0.0-RC1-final/update1 and on ASP.NET 5 1.0.0-RC1-final/update1.  We will update to RC2 when it becomes stable.

# Usage
See the Readme for each provider for more details on how to make use of it in an application.

# Nuget Feeds
All new configuration provider development is done on the dev branch. More stable versions of the providers can be found on the master branch. The latest prebuilt packages from each branch can be found on one of two MyGet feeds:

[Development feed (Less Stable)](https://www.myget.org/gallery/steeltoedev) - https://www.myget.org/gallery/steeltoedev

[Master feed (Stable)](https://www.myget.org/gallery/steeltoemaster) - https://www.myget.org/gallery/steeltoemaster

Release or release candidate packages can be found on [nuget.org](https://www.nuget.org/). Currently there are none available.  We anticipate creatiing a Release Candidate soon.

# Building Packages & Running Tests - Windows
To build the packages on windows:

```
1. git clone ...
2. cd <clone directory>
3. Install DNX 1.0.0-rc1-final/update1. Install both the coreclr and clr runtimes. 
4. Add a DNX runtime to your path. (e.g. dnvm use 1.0.0-rc1-update1 -r clr)
5. dnu restore src
6. cd src\<project> (e.g. cd src\SteelToe.Extensions.Configuration.CloudFoundry)
7. dnu pack --configuration <Release or Debug> 
```
The resulting artifacts can be found in the bin folder under the corresponding project. (e.g. src\SteelToe.Extensions.Configuration.CloudFoundry/bin

To run the unit tests:
```
1. git clone ...
2. cd <clone directory>
3. Install DNX 1.0.0-rc1-final/update1. Install the runtime/arch you want to run the unit tests on.
4. Add the DNX runtime to your path. (e.g. dnvm use 1.0.0-rc1-update1 -r clr -a x86)
5. dnu restore test
6. cd test\<test project> (e.g. cd test\SteelToe.Extensions.Configuration.CloudFoundry.Test)
7. dnx test
```
# Building Packages & Running Tests - Linux/OSX
To build the packages on Linux/OSX:

```
1. git clone ...
2. cd <clone directory>
3. Install DNX 1.0.0-rc1-final/update1
4. Add the DNX runtime to your path. (i.e. dnvm use 1.0.0-rc1-update1 -r coreclr -a x64)
3. dnu restore src
4. cd src/<project> (e.g.. cd src/SteelToe.Extensions.Configuration.CloudFoundry)
5. dnu pack --framework dnxcore50 --configuration <Release or Debug> 
```
The resulting artifacts can be found in the bin folder under the corresponding project. (e.g. src/SteelToe.Extensions.Configuration.CloudFoundry/bin

To run the unit tests:
```
1. git clone ...
2. cd <clone directory>
3. Install DNX 1.0.0-rc1-final/update1
4. Add the DNX runtime to your path. (i.e. dnvm use 1.0.0-rc1-update1 -r coreclr -a x64)
5. dnu restore test
6. cd test/<test project> (e.g. cd test/SteelToe.Extensions.Configuration.CloudFoundry.Test)
7. dnx test
```