# Steeltoe Open Source Software

[![Build Status](https://dev.azure.com/SteeltoeOSS/Steeltoe/_apis/build/status/SteeltoeOSS.steeltoe?branchName=master)](https://dev.azure.com/SteeltoeOSS/Steeltoe/_build/latest?definitionId=4&branchName=master)

* [Introduction](#introduction)
* [Project Management](#project-management)
* [Project Repositories](#project-repositories)
* [Roadmaps](#roadmaps)
* [Release Types](#release-types)
* [Getting Started](#getting-started)
* [Getting the Code](#getting-the-code)
* [Contributing](#contributing)
* [Governance Model](#governance-model)
* [Licenses](#licenses)

## Introduction

Steeltoe is an open source project aimed at developing cloud native .NET microservice applications.  This project provides libraries that follow similar development patterns from well-known and proven microservice libraries like Netflix OSS, Spring Cloud and others.

Steeltoe libraries are built on top of .NET APIs, following the .NET Standard 2.0 specification. Therefore, Steeltoe allows you work with .NET Core and .NET Framework 4.x.

Today, most Steeltoe components work in a stand-alone environment as well other PaaS implementations.

Steeltoe components typically build on other technology offerings, such as Netflix OSS and Spring Cloud by providing several packages that enable .NET developers to quickly leverage these tools when implementing some of the basic patterns (for example: centralized configuration management, service discovery, circuit breakers, etc.) typically found in highly scalable and resilient distributed applications.

Steeltoe provides services that broadly fall into two categories:

* Services that simplify using .NET and ASP.NET on cloud platforms like Cloud Foundry:
  * Connectors (MySql, PostgreSQL, Microsoft SQL Server, RabbitMQ, Redis, OAuth, etc)
  * Configuration
  * Security (OAuth SSO, JWT, Redis Key Ring Storage, etc.)
  * Logging

* Services that enable .NET and ASP.NET developers to leverage Netflix OSS, Spring Cloud and other industry leading services:
  * Configuration providers (Spring Cloud, Vault, etc.)
  * Service Discovery client (Netflix Eureka, etc.)
  * CircuitBreaker (Netflix Hystrix, etc.)
  * Management

[Steeltoe is freely available](https://www.nuget.org/packages?q=steeltoe) for production application usage today. Be sure to visit the [official Steeltoe site](https://steeltoe.io/).

## Project Management

The Steeltoe team uses [ZenHub](https://app.zenhub.com/workspaces/steeltoe-project-5d92153f31e8dd0001222ae6/board) for our Project Management Board across our active repositories.  This is the best way to see what state each issue is in, along with issue priorization and release assignment.

_Note:_ ZenHub is built on top of GitHub issues, so it reflects the updates on GitHub as well as providing a view of the project's progress.

## Project Repositories

Steeltoe is fully open source and is found under the SteeltoeOSS organization on GitHub.

### Steeltoe Core Components

These are located in the [Steeltoe](https://github.com/SteeltoeOSS/steeltoe) repository:

* Configuration - configuration providers which extend the reach of [.NET Configuration](https://github.com/dotnet/extensions/tree/master/src/Configuration) services
* Common - Common packages to other Steeltoe components
* CircuitBreaker - monitor and isolate requests to remote dependent services with latency and fault tolerance logic
* Connectors - simplify the process of configuring and using back-end services locally and in the cloud
* Discovery - provide the ability to register and discover services locally and in the cloud
* Logging - adds logging extensions
* Management - add monitoring and management to production based application
* Security - simplify integration of security services provided by the cloud platform

#### Other Repositories

* [Samples](https://github.com/SteeltoeOSS/Samples) - Our collection of Sample applications used as a reference for Steeltoe .NET Application development
* [Documentation](https://github.com/SteeltoeOSS/Documentation) - Markdown versions of our documentation on the [steeltoe.io/docs](steeltoe.io/docs) site
* [MainSite](https://github.com/SteeltoeOSS/MainSite) - All of the steeltoe.io (Blazor) website
* [Initializr](https://github.com/SteeltoeOSS/initializr) - The [Steeltoe Initializr](https://start.steeltoe.io) source code
* [Tooling](https://github.com/SteeltoeOSS/Tooling) - Steeltoe SDK and Tooling
* [Dockerfiles](https://github.com/SteeltoeOSS/Dockerfiles) - Our collection of docker files we have on dockerhub
* [eShopOnContainers](https://github.com/SteeltoeOSS/eShopOnContainers) - Sample reference microservice and container based application with added Steeltoe capabilities (Forked and updated from dotnet-architecture org)


## Roadmaps

* [3.1.0](roadmaps/3.1.0.md) - In Progress (expected by end of 2020)
* [3.0.0](roadmaps/3.0.0.md) - Released (8/2020)
* [2.5.0](roadmaps/2.5.0.md) - In Progress (expected 9/2020)
* [2.4.0](roadmaps/2.4.0.md) - Released (11/2019)
   * [2.4.1](https://github.com/SteeltoeOSS/steeltoe/releases/tag/2.4.1) - Released (1/2020)
   * [2.4.2](https://github.com/SteeltoeOSS/steeltoe/releases/tag/2.4.2) - Released (2/2020)
   * [2.4.3](https://github.com/SteeltoeOSS/steeltoe/releases/tag/2.4.3) - Released (4/2020)
* [2.3.0](roadmaps/2.3.0.md) - Released (8/2019)
* [2.2.0](roadmaps/2.2.0.md) - Released (3/2019)
* [2.1.0](roadmaps/2.1.0.md) - Released (8/2018)
* [2.0.0](roadmaps/2.0.0.md) - Released (2/2018)

## Release Types

* **Milestone** (i.e. M1, M2 ...) - A milestone is a release that contains a subset of the full features planned for a release. Milestone releases allow major features to be tested earlier in the release cycle.  A milestone release is not meant for production environments.
* **Release Candidate** (i.e. RC1, RC2, ...) - Similar to a beta release.  A release candidate contains a full feature set and is available to the community for finding any major issues before the GA version is released.  There could still be source code changes to fix defects, changes to documentation, and test cases, but these releases are considered code complete. Typically, these are not meant for production environments.
* **General Availability** (i.e. GA) - This is the full featured and fully tested release that is available to the public for use in production environments.

## Getting Started

1. Follow any "Get Started" link on the [Steeltoe site](https://steeltoe.io/)
1. Review, run, and modify the extensive collection of [Samples](https://github.com/SteeltoeOSS/Samples) available on Github
1. To get down into the details of any Steeltoe project, read the [documentation](https://steeltoe.io/docs/)

## Contributing

The Steeltoe project welcomes contributions on GitHub both by filing issues and through PRs. You are also welcome to join our discussions on [Slack](https://slack.steeltoe.io/)

Check out the [contributing guidelines](https://github.com/SteeltoeOSS/.github/blob/master/CONTRIBUTING.md) page to see how you can get involved and contribute to Steeltoe.

Also its worth noting, the Steeltoe project has adopted the code of conduct defined by the [Contributor Covenant](https://contributor-covenant.org/).
If you'd like more information, see the [.NET Foundation Code of Conduct](https://www.dotnetfoundation.org/code-of-conduct) write-up.

## Getting the Code

The development of the core components of Steeltoe is done out of the [steeltoe](/) repository on the `master` branch.

Maintenance branches are created after each major release (i.e. 2.x) and minor branches (i.e. 2.2.x) are created as needed for regressions, and/or security issues.

All release and release candidate packages are listed under the tags section on GitHub (e.g. 2.2.0).

The latest Steeltoe packages from each branch can be found on [MyGet](https://myget.org/gallery/steeltoedev).

The released and release candidates can be found on [NuGet](https://www.nuget.org/profiles/steeltoe).

## Building from Source

Steeltoe components can be built from source with a recent version of the [.NET Core SDK](https://dotnet.microsoft.com/download/visual-studio-sdks) on Windows, Linux or MacOS. Any IDE that supports .NET is expected to work - core contributors regularly use Visual Studio, Visual Studio Code and JetBrains Rider. Visual Studio Solution (`.sln`) files are provided for each component, along with a single `.sln` file that will load all components.

## Governance Model

As a member of the [.NET Foundation](https://dotnetfoundation.org/), the Steeltoe project has adopted a [project governance](https://github.com/dotnet/home/blob/master/governance/project-governance.md) model in line with that recommended by the Foundation.

## Licenses

The Steeltoe project uses the [Apache License Version 2.0](https://github.com/SteeltoeOSS/.github/blob/master/LICENSE.md) license for all of its code.  See the [contribution licensing](https://github.com/SteeltoeOSS/.github/blob/master/contributing-docs/contributing-license.md) document for more details.
