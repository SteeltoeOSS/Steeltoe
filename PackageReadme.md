# Steeltoe

[Steeltoe](https://steeltoe.io/) provides building blocks for development of .NET applications that integrate with [Spring](https://spring.io/) and [Spring Boot](https://spring.io/projects/spring-boot) environments, as well as [Cloud Foundry](https://www.cloudfoundry.org/) and [Kubernetes](https://kubernetes.io/) with first-party support for [Tanzu](https://tanzu.vmware.com/tanzu).

Key features include:

- External (optionally encrypted) configuration using [Spring Cloud Config Server](https://docs.spring.io/spring-cloud-config/docs/current/reference/html/)
- Service discovery with [Netflix Eureka](https://spring.io/projects/spring-cloud-netflix) and [HashiCorp Consul](https://www.consul.io/)
- Management endpoints (compatible with [actuators](https://docs.spring.io/spring-boot/docs/current/reference/html/actuator.html)), providing system info (such as versions, configuration, service container contents, mapped routes and HTTP traffic), heap/thread dumps, health checks, exporting metrics to [Prometheus](https://prometheus.io/), and changing log levels at runtime.
- Connectivity to databases (such as [SQL Server](https://www.microsoft.com/sql-server)/[Azure SQL](https://azure.microsoft.com/products/azure-sql), [Cosmos DB](https://azure.microsoft.com/products/cosmos-db/), [MongoDB](https://www.mongodb.com/), [Redis](https://redis.io/), [RabbitMQ](https://www.rabbitmq.com/), [PostgreSQL](https://www.postgresql.org/), and [MySQL](https://www.mysql.com/)), including support for [Entity Framework Core](https://learn.microsoft.com/ef/core/)
- Single sign-on, JWT and Certificate auth with [Cloud Foundry](https://www.cloudfoundry.org/)

## Getting Started

In addition to the [documentation site](https://docs.steeltoe.io), we have built several tools to help you get started:

- [Steeltoe Initializr](https://start.steeltoe.io) - Pick and choose what type of application you would like to build and let us generate the initial project for you
  - The Initializr uses [.NET templates](https://github.com/SteeltoeOSS/NetCoreToolTemplates) that can also be used from the `dotnet` CLI and inside of Visual Studio
- [Steeltoe Samples](https://github.com/SteeltoeOSS/Samples) - Here we have working samples for trying out features and to use as code references

### Framework Targets

| Steeltoe Version | .NET Version |
| --- | --- |
| 4.x | .NET 8 - 9 |
| 3.x | .NET Core 3.1 - .NET 6 |
| 2.x | .NET Framework 4.6.1+ |

## Feedback and Support

For community support, we recommend [Steeltoe OSS Slack](https://slack.steeltoe.io) or [StackOverflow](https://stackoverflow.com/questions/tagged/steeltoe)

For production support, we recommend you reach out to [Broadcom Support](https://support.broadcom.com/).

For other questions or feedback, [open an issue](https://github.com/SteeltoeOSS/Steeltoe/issues/new/choose).

## Contributing

Bug reports and contributions are welcome at [the GitHub repository](https://github.com/SteeltoeOSS/Steeltoe).

For more information on contributing to the project, please see the [Steeltoe Wiki](https://github.com/SteeltoeOSS/Steeltoe/wiki#contributing).
