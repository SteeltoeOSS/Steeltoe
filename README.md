# .NET Service Discovery & Registration
A Service Registry provides a database which applications can use in implementing the Service Discovery pattern; one of the key tenets of a microservice-based architecture. Trying to hand-configure each client of a service or adopt some form of access convention can be difficult and prove to be brittle in production. Instead, your applications can use a Service Registry to dynamically discover and call registered services.

There are several popular options for Service Registries. Netflix built and then open-sourced their own service registry, Eureka. Another relatively new, but increasingly popular option is Consul. 

This repo contains various packages for interacting with Service Registries.  The [SteelToe.Discovery.Eureka.Client](https://github.com/SteelToeOSS/Discovery/tree/master/src/SteelToe.Discovery.Eureka.Client) enables using [Spring Cloud Eureka Server](http://projects.spring.io/spring-cloud/docs/1.0.3/spring-cloud.html#spring-cloud-eureka-server) as a Service Registry while [SteelToe.Discovery.Client](https://github.com/SteelToeOSS/Discovery/tree/master/src/SteelToe.Discovery.Client) provides a configurable generalized interface to Service Discovery and Registration.  Normally you will want to use the `SteelToe.Discovery.Client` and configure it to work with the Service Registry (i.e. Eureka, Consul) you intend to use in your app. Currently the client only supports Eureka, but in the near future support will be added for Consul.

Windows Master:  [![AppVeyor Master](https://ci.appveyor.com/api/projects/status/j6i5gxxwt21gys01/branch/master?svg=true)](https://ci.appveyor.com/project/steeltoe/discovery/branch/master)

Windows Dev:  [![AppVeyor Dev](https://ci.appveyor.com/api/projects/status/j6i5gxxwt21gys01/branch/dev?svg=true)](https://ci.appveyor.com/project/steeltoe/discovery/branch/dev)

Linux/OS X Master: [![Travis Master](https://travis-ci.org/SteelToeOSS/Discovery.svg?branch=master)](https://travis-ci.org/SteelToeOSS/Discovery)

Linux/OSX Dev: [![Travis Dev](https://travis-ci.org/SteelToeOSS/Discovery.svg?branch=dev)](https://travis-ci.org/SteelToeOSS/Discovery)

# .NET Runtime & Framework Support
The packages are intended to support both .NET 4.5.1+ and .NET Core (CoreCLR/CoreFX) runtimes.  They are built and unit tested on Windows, Linux and OSX.

While the primary usage of the packages is intended to be with ASP.NET 5 applications, they should also work fine with UWP, Console and ASP.NET 4.x apps. 

Currently they have been tested on DNX 1.0.0-RC1-final/update1 (CoreCLR & 4.5.1+) and on ASP.NET 5 1.0.0-RC1-final/update1.  We will update to DotNetCLI and ASP.NET RC2 when it becomes stable.

# Usage
See the Readme for each enclosed project for more details on how to make use of it in an application.

# Nuget Feeds
All new development is done on the dev branch. More stable versions of the packages can be found on the master branch. The latest prebuilt packages from each branch can be found on one of two MyGet feeds. Released version can be found on nuget.org.

[Development feed (Less Stable)](https://www.myget.org/gallery/steeltoedev) - https://www.myget.org/gallery/steeltoedev

[Master feed (Stable)](https://www.myget.org/gallery/steeltoemaster) - https://www.myget.org/gallery/steeltoemaster

[Release or Release Candidate feed](https://www.nuget.org/) - https://www.nuget.org/. 

# Building Packages & Running Tests - Windows
To build the packages on windows:

1. git clone ...
2. cd <clone directory>
3. Install DNX 1.0.0-rc1-final/update1. Install both the coreclr and clr runtimes. 
4. Add a DNX runtime to your path. (e.g. dnvm use 1.0.0-rc1-update1 -r clr)
5. dnu restore src
6. cd src\<project> (e.g. cd src\SteelToe.Discovery.Client)
7. dnu pack --configuration <Release or Debug> 

The resulting artifacts can be found in the bin folder under the corresponding project. (e.g. src\SteelToe.Extensions.Configuration.CloudFoundry/bin

To run the unit tests:

1. git clone ...
2. cd <clone directory>
3. Install DNX 1.0.0-rc1-final/update1. Install the runtime/arch you want to run the unit tests on.
4. Add the DNX runtime to your path. (e.g. dnvm use 1.0.0-rc1-update1 -r clr -a x86)
5. dnu restore test
6. cd test\<test project> (e.g. cd test\SteelToe.Discovery.Client.Test)
7. dnx test

# Building Packages & Running Tests - Linux/OSX
To build the packages on Linux/OSX:

1. git clone ...
2. cd <clone directory>
3. Install DNX 1.0.0-rc1-final/update1
4. Add the DNX runtime to your path. (i.e. dnvm use 1.0.0-rc1-update1 -r coreclr -a x64)
3. dnu restore src
4. cd src/<project> (e.g.. cd src/SteelToe.Discovery.Client)
5. dnu pack --framework dnxcore50 --configuration <Release or Debug> 

The resulting artifacts can be found in the bin folder under the corresponding project. (e.g. src/SteelToe.Extensions.Configuration.CloudFoundry/bin

To run the unit tests:

1. git clone ...
2. cd <clone directory>
3. Install DNX 1.0.0-rc1-final/update1
4. Add the DNX runtime to your path. (i.e. dnvm use 1.0.0-rc1-update1 -r coreclr -a x64)
5. dnu restore test
6. cd test/<test project> (e.g. cd test/SteelToe.Discovery.Client.Test)
7. dnx test

# Sample Applications
See the [Samples](https://github.com/SteelToeOSS/Samples) repo for examples of how to use these packages.
