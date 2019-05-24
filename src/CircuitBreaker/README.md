# .NET Circuit Breaker Frameworks

Cloud-native architectures are typically composed of multiple layers of distributed services. End-user requests may comprise multiple calls to these services, and if a lower-level service fails, the failure can cascade up to the end user and spread to other dependent services. Heavy traffic to a failing service can also make it difficult to repair. Using Circuit Breaker patterns, you can prevent failures from cascading and provide fallback behavior until a failing service is restored to normal operation.

When applied to a service, a circuit breaker framework watches for failing calls to the service. If failures reach a certain threshold, it “opens” the circuit and automatically redirects calls to the specified fallback mechanism. This gives the failing service time to recover.

There are several popular Circuit Breaker framework options for .NET . Netflix built and then open-sourced their own Circuit Breaker framework, Hystrix - Neflix's latency and fault-tolerence library. Another heavily used option in the .NET space is Polly.

This repository contains various packages for implementing the Circuit breaker pattern in .NET and ASP.NET applications.  The [Steeltoe.CircuitBreaker.Hystrix.Core](https://github.com/SteeltoeOSS/CircuitBreaker/tree/master/src/Steeltoe.CircuitBreaker.Hystrix.Core) package is a port of the core [Netflix Hystrix](https://github.com/Netflix/Hystrix) Circuit Breaker framework to .NET. The [Steeltoe.CircuitBreaker.Hystrix](https://github.com/SteeltoeOSS/CircuitBreaker/tree/master/src/Steeltoe.CircuitBreaker.Hystrix) package adds some additional helper methods to make it easy to incorporate Hystrix into your ASP.NET application. Typically you will reference this package, instead of the Core package in your .csproj file.

Additionally, two additional packages are included each of which help you use the [Hystrix Dashboard](https://github.com/Netflix/Hystrix/wiki/Dashboard) to monitor your applications circuits and gather Hystrix metrics in real time. The [Steeltoe.CircuitBreaker.Hystrix.MetricsEvents](https://github.com/SteeltoeOSS/CircuitBreaker/tree/master/src/Steeltoe.CircuitBreaker.Hystrix.MetricsEvents) package enables using the open source [Netflix Hystrix Dashboard](https://github.com/Netflix/Hystrix/wiki/Dashboard) when monitoring your ASP.NET application. You simply include this package in your application and then point the Netflix Dashboard at the app in order to begin seeing Hystrix Metrics.

The other dashboard releated package is the [Steeltoe.CircuitBreaker.Hystrix.MetricsStream](https://github.com/SteeltoeOSS/CircuitBreaker/tree/dev/src/Steeltoe.CircuitBreaker.Hystrix.MetricsStream) package.  It enables using the Spring Cloud Services [Hystrix Dashboard](https://docs.pivotal.io/spring-cloud-services/1-3/common/circuit-breaker) on Cloud Foundry for monitoring your application. In order to use it, you include this package into your application and then bind the Spring Cloud Services Hystrix Dashboard to your app to begin streaming metrics to the dashboard.

## .NET Runtime & Framework Support

The packages are intended to support both .NET 4.6+ and .NET Core (CoreCLR/CoreFX) runtimes.  They are built and unit tested on Windows, Linux and OSX.

While the primary usage of the providers is intended to be with ASP.NET Core applications, they should also work fine with UWP, Console and ASP.NET 4.x apps.

Currently all of the code and samples have been tested on .NET Core 2.0, .NET 4.6.x, and on ASP.NET Core 2.0.0.

## Usage

For more information on how to use these components see the online [Steeltoe documentation](https://steeltoe.io/).

## Nuget Feeds

All new development is done on the dev branch. More stable versions of the packages can be found on the master branch. The latest prebuilt packages from each branch can be found on one of two MyGet feeds. Released version can be found on nuget.org.

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
1. cd clone directory
1. cd src/project (e.g. cd src/Steeltoe.CircuitBreaker.HystrixBase)
1. dotnet restore
1. dotnet pack --configuration Release or Debug

The resulting artifacts can be found in the bin folder under the corresponding project. (e.g. src/Steeltoe.CircuitBreaker.HystrixBase/bin

To run the unit tests:

1. git clone ...
1. cd clone directory
1. cd test/test project (e.g. cd test/Steeltoe.CircuitBreaker.HystrixBase.Test)
1. dotnet restore
1. dotnet xunit -verbose

## Building Packages & Running Tests - Linux/OSX

To build the packages on Linux/OSX:

1. git clone ...
1. cd clone directory
1. cd src/project (e.g.. cd src/Steeltoe.CircuitBreaker.HystrixBase)
1. dotnet restore
1. dotnet pack --configuration Release or Debug

The resulting artifacts can be found in the bin folder under the corresponding project. (e.g. src/Steeltoe.CircuitBreaker.HystrixBase/bin

To run the unit tests:

1. git clone ...
1. cd clone directory
1. cd test/test project (e.g. cd test/Steeltoe.CircuitBreaker.HystrixBase.Test)
1. dotnet restore
1. dotnet xunit -verbose -framework netcoreapp2.0

## Sample Applications

See the [Samples](https://github.com/SteeltoeOSS/Samples) repo for examples of how to use these packages.
