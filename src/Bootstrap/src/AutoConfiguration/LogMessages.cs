// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Bootstrap.AutoConfiguration;

internal static class LogMessages
{
    public const string WireAllActuators = "Configured actuators";
    public const string WireKubernetesActuators = "Configured Kubernetes actuators";

    public const string WireCloudFoundryConfiguration = "Configured Cloud Foundry configuration provider";
    public const string WireKubernetesConfiguration = "Configured Kubernetes configuration provider";
    public const string WireConfigServerConfiguration = "Configured Config Server configuration provider";
    public const string WirePlaceholderConfiguration = "Configured placeholder configuration provider";
    public const string WireRandomValueConfiguration = "Configured random value configuration provider";

    public const string WireCloudFoundryContainerIdentity = "Configured Cloud Foundry MTLs security";
    public const string WireDiscoveryClient = "Configured discovery client";
    public const string WireDistributedTracing = "Configured distributed tracing";
    public const string WireDynamicSerilog = "Configured dynamic console logger for Serilog";
    public const string WireWavefrontMetrics = "Configured Wavefront metrics";
    public const string WirePrometheus = "Configured Prometheus";

    public const string WireCosmosDbConnector = "Configured CosmosDB connector";
    public const string WireMongoDbConnector = "Configured MongoDB connector";
    public const string WireMySqlConnector = "Configured MySQL connector";
    public const string WirePostgreSqlConnector = "Configured PostgreSQL connector";
    public const string WireRabbitMQConnector = "Configured RabbitMQ connector";
    public const string WireStackExchangeRedisConnector = "Configured StackExchange Redis connector";
    public const string WireDistributedCacheRedisConnector = "Configured Redis distributed cache connector";
    public const string WireSqlServerConnector = "Configured SQL Server connector";
}
