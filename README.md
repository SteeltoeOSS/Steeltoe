# Steeltoe .NET Open Source Software

[![Build Status](https://dev.azure.com/SteeltoeOSS/Steeltoe/_apis/build/status/Steeltoe.All?branchName=main)](https://dev.azure.com/SteeltoeOSS/Steeltoe/_build/latest?definitionId=4&branchName=main)[![NuGet Version](https://img.shields.io/nuget/v/Steeltoe.Common.svg?style=flat)](https://www.nuget.org/packages/Steeltoe.Common/)&nbsp;[![Stack Overflow](https://img.shields.io/badge/stack%20overflow-steeltoe-orange.svg)](http://stackoverflow.com/questions/tagged/steeltoe)

## Why Steeltoe?

Are you looking to create .NET microservices? Modernizing existing applications? Moving apps to containers? Steeltoe can help you!

[Steeltoe](https://steeltoe.io) is an open-source project providing a library collection that helps users build production-grade cloud-native applications using externalized configuration, database connectors, service discovery, logging and distributed tracing, application management, security, and more.

We have also built several tools to get you started:

* [Steeltoe Initializr](https://start.steeltoe.io) - Pick and choose what type of application you would like to build and let us generate the initial project for you
  * Not only have we built Steeltoe Initializr site, but our templates are also available as an option from the `dotnet` CLI
  * We also have the ability to load these project templates inside of Visual Studio
* [Steeltoe Samples](https://github.com/SteeltoeOSS/Samples) - Here we have working samples for trying out features and to use as code references

## Pre-release packages

Whether you are working with the team on validating a bugfix or just want to try the latest version available, you can use the Steeltoe development feed by adding a reference in your nuget.config file:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="Steeltoe-dev" value="https://pkgs.dev.azure.com/dotnet/Steeltoe/_packaging/dev/nuget/v3/index.json" />
    <add key="NuGet" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

## Project Information

For more project information, please see the [Steeltoe Wiki](https://github.com/SteeltoeOSS/Steeltoe/wiki).

See our [website](https://steeltoe.io) for lots of information, blogs, documentation, and getting started guides.

For community support, we recommend [Steeltoe OSS Slack](https://slack.steeltoe.io) or [StackOverflow](https://stackoverflow.com/questions/tagged/steeltoe)

For production support, we recommend you reach out to [Broadcom Support](https://support.broadcom.com/).

For other questions or feedback, [open an issue](https://github.com/SteeltoeOSS/Steeltoe/issues/new/choose).

### Conduct

This project has adopted the code of conduct defined by the Contributor Covenant to clarify expected behavior in our community.
For more information see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct).
