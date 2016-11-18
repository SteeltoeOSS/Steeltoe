# .NET Configuration Providers

With the introduction of ASP.NET Core, Microsoft is providing a new [application configuration model](https://docs.asp.net/en/latest/fundamentals/configuration.html) for accessing configuration settings for an application. This new model supports access to key/value configuration data from a variety of different configuration providers or sources. Out of the box, ASP.NET Core comes with support for [JSON](https://github.com/aspnet/Configuration/tree/dev/src/Microsoft.Extensions.Configuration.Json), [XML](https://github.com/aspnet/Configuration/tree/dev/src/Microsoft.Extensions.Configuration.Xml) and [INI](https://github.com/aspnet/Configuration/tree/dev/src/Microsoft.Extensions.Configuration.Ini) files, as well as environment variables and command line parameters.  Additionally, Microsoft has also enabled developers to write their own [custom configuration providers](https://docs.asp.net/en/latest/fundamentals/configuration.html#custom-config-providers) should those provided by Microsoft not meet your needs.  

This repo contains two custom configuration providers.  The [Steeltoe.Extensions.Configuration.ConfigServer](https://github.com/SteeltoeOSS/Configuration/tree/master/src/Steeltoe.Extensions.Configuration.ConfigServer) enables using the [Spring Cloud Config Server](https://projects.spring.io/spring-cloud/) as a provider of configuration data and the [Steeltoe.Extensions.Configuration.CloudFoundry](https://github.com/SteeltoeOSS/Configuration/tree/master/src/Steeltoe.Extensions.Configuration.CloudFoundry) provider enables [CloudFoundry environment variables](https://docs.cloudfoundry.org) to be parsed and accessed as configuration data.

Windows Master (Stable):  [![AppVeyor Master](https://ci.appveyor.com/api/projects/status/27c2hd0460aac1cs/branch/master?svg=true)](https://ci.appveyor.com/project/steeltoe/Configuration)

Windows Dev (Less Stable):  [![AppVeyor Dev](https://ci.appveyor.com/api/projects/status/27c2hd0460aac1cs/branch/dev?svg=true)](https://ci.appveyor.com/project/steeltoe/Configuration)

Linux/OS X Master (Stable): [![Travis Master](https://travis-ci.org/SteeltoeOSS/Configuration.svg?branch=master)](https://travis-ci.org/SteeltoeOSS/Configuration)

Linux/OS X Dev (Less Stable): [![Travis Dev](https://travis-ci.org/SteeltoeOSS/Configuration.svg?branch=dev)](https://travis-ci.org/SteeltoeOSS/Configuration)

# .NET Runtime & Framework Support
Like the ASP.NET Core configuration providers, these providers are intended to support both .NET 4.5.1+ and .NET Core (CoreCLR/CoreFX) runtimes.  The providers are built and unit tested on Windows, Linux and OSX.

While the primary usage of the providers is intended to be with ASP.NET Core applications, they should also work fine with UWP, Console and ASP.NET 4.x apps. An ASP.NET 4.x sample app is available illustrating how this can be done.

Currently all of the code and samples have been tested on .NET Core 1.1, .NET 4.5.1/4.6.x, and on ASP.NET Core 1.1.0.

# Usage
See the Readme for each provider for more details on how to make use of it in an application.

# Nuget Feeds
All new configuration provider development is done on the dev branch. More stable versions of the providers can be found on the master branch. The latest prebuilt packages from each branch can be found on one of two MyGet feeds. Released version can be found on nuget.org.

[Development feed (Less Stable)](https://www.myget.org/gallery/steeltoedev) - https://www.myget.org/gallery/steeltoedev

[Master feed (Stable)](https://www.myget.org/gallery/steeltoemaster) - https://www.myget.org/gallery/steeltoemaster

[Release or Release Candidate feed](https://www.nuget.org/) - https://www.nuget.org/. 

# Building Packages & Running Tests - Windows
To build the packages on windows:

1. git clone ...
2. cd <clone directory>
3. Install .NET Core SDK
4. dotnet restore src
5. cd src\<project> (e.g. cd src\Steeltoe.Extensions.Configuration.CloudFoundry)
6. dotnet pack --configuration Release or Debug

The resulting artifacts can be found in the bin folder under the corresponding project. (e.g. src\Steeltoe.Extensions.Configuration.CloudFoundry\bin

To run the unit tests:

1. git clone ...
2. cd <clone directory>
3. Install .NET Core SDK 
4. dotnet restore test
5. cd test\<test project> (e.g. cd test\Steeltoe.Extensions.Configuration.CloudFoundry.Test)
6. dotnet test 

# Building Packages & Running Tests - Linux/OSX
To build the packages on Linux/OSX:

1. git clone ...
2. cd <clone directory>
3. Install .NET Core SDK
4. dotnet restore src
5. cd src/<project> (e.g.. cd src/Steeltoe.Extensions.Configuration.CloudFoundry)
6. dotnet pack --configuration Release or Debug

The resulting artifacts can be found in the bin folder under the corresponding project. (e.g. src/Steeltoe.Extensions.Configuration.CloudFoundry/bin

To run the unit tests:

1. git clone ...
2. cd <clone directory>
3. Install .NET Core SDK 
4. dotnet restore test
5. cd test\<test project> (e.g. cd test/Steeltoe.Extensions.Configuration.CloudFoundry.Test)
6. dotnet test --framework netcoreapp1.1

# Sample Applications
See the [Samples](https://github.com/SteeltoeOSS/Samples) repo for examples of how to use these packages.

# Known limitations

### Using Options and Configuration
When using the Microsoft provided [Option extension framework](https://docs.asp.net/en/latest/fundamentals/configuration.html?highlight=ioptions#using-options-and-configuration-objects) you will find that the Options POCO does not update with new values if the configuration is refreshed with new values from the Config server. This is a [known limitation of the Options framework](https://github.com/aspnet/Options/issues/145).

If you retrieve values from the `IConfiguration` directly, you will see the updated values, they just will not be reflected in the Options POCO.

Example:

Having configured a `ConfigServerData` POCO through IOC

```
public IConfigurationRoot Configuration { get; }
...
services.Configure<ConfigServerData>(Configuration);
```
If `ConfigServerData.SomeProperty` has an initial value of `foo` after startup, and then you change the value for the property from `foo` to `bar` in the Config Server Git repo and then somewhere in your code you call:
```
Configurtion.Reload();
```
you will find that `ConfigServerData.SomeProperty` will still have the value of `foo`. But, if you directly reference the `IConfiguration`, you will see the updated property value.

```
var value = Configuration["SomeProperty"]; // value == 'bar'
```

This is a [known limitation of the Options framework](https://github.com/aspnet/Options/issues/145).

### Unstructured data files
Unlike the Java version of the configuration server client, the Steeltoe client currently only supports property and yaml files; not plain text.

### Client decryption
Steeltoe client only supports clear text communication with the configuration server. Client decryption is on our roadmap, but not currently supported. For now, you cannot send encrypted data to the client.

### Server initiated reload
Currently reloads must be initiated by the client, Steeltoe has not implemented handlers to listen for server change events.
