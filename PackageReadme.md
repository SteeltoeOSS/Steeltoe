[Steeltoe](https://steeltoe.io/) provides building blocks for development of ASP.NET Core Web APIs and microservices that integrate with [Java Spring](https://spring.io/) and [Spring Boot](https://spring.io/projects/spring-boot) environments, as well as [Cloud Foundry](https://www.cloudfoundry.org/) and [Kubernetes](https://kubernetes.io/) ([Tanzu](https://tanzu.vmware.com/tanzu)).

Features include:
- Management endpoints ([actuators](https://docs.spring.io/spring-boot/docs/current/reference/html/actuator.html)), providing system info, heap/thread dumps, health checks, and changing log levels at runtime
- Connectivity to databases ([SQL Server](https://www.microsoft.com/en-us/sql-server)/[Azure SQL](https://azure.microsoft.com/en-us/products/azure-sql), [Cosmos DB](https://azure.microsoft.com/en-us/products/cosmos-db/), [MongoDB](https://www.mongodb.com/), [Redis](https://redis.io/), [RabbitMQ](https://www.rabbitmq.com/), [PostgreSQL](https://www.postgresql.org/), and [MySQL](https://www.mysql.com/)), including support for [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- Distributed tracing and metrics exporters for [Prometheus](https://prometheus.io/) and [Wavefront](https://docs.wavefront.com/tracing_basics.html)
- Service discovery with [Eureka](https://www.tutorialspoint.com/spring_boot/spring_boot_eureka_server.htm) and [Consul](https://www.consul.io/)
- External (encrypted) configuration using [Spring Config Server](https://docs.spring.io/spring-cloud-config/docs/current/reference/html/)
- Single sign-on with [Cloud Foundry](https://www.cloudfoundry.org/)
- An easy-to-use API for [RabbitMQ message exchange](https://www.rabbitmq.com/features.html), providing [RabbitTemplate](https://spring.io/guides/gs/messaging-rabbitmq/) and annotation-driven listeners
- Streaming with [Spring Cloud Data Flow](https://spring.io/projects/spring-cloud-dataflow), enabling orchestration with Java stream apps
- Steeltoe converts [Spring Expression Language (SpEL)](https://docs.spring.io/spring-framework/docs/3.0.x/reference/expressions.html) to .NET compiled code
