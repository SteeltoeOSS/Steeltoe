// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry.ServiceBindings;

/// <summary>
/// Lists the built-in Cloud Foundry service brokers.
/// </summary>
[Flags]
public enum CloudFoundryServiceBrokerTypes
{
    /// <summary>
    /// Don't use any of the built-in brokers.
    /// </summary>
    None = 0x0,

    /// <summary>
    /// Use the built-in brokers for Netflix Eureka.
    /// </summary>
    Eureka = 0x1,

    /// <summary>
    /// Use the built-in brokers for JWT and OpenID Connect.
    /// </summary>
    Identity = 0x2,

    /// <summary>
    /// Use the built-in brokers for MongoDB.
    /// </summary>
    MongoDb = 0x4,

    /// <summary>
    /// Use the built-in brokers for MySQL.
    /// </summary>
    MySql = 0x8,

    /// <summary>
    /// Use the built-in brokers for PostgreSQL.
    /// </summary>
    PostgreSql = 0x10,

    /// <summary>
    /// Use the built-in brokers for RabbitMQ.
    /// </summary>
    RabbitMQ = 0x20,

    /// <summary>
    /// Use the built-in brokers for Redis/Valkey.
    /// </summary>
    Redis = 0x40,

    /// <summary>
    /// Use the built-in brokers for Microsoft SQL Server.
    /// </summary>
    SqlServer = 0x80,

    /// <summary>
    /// Use all built-in brokers.
    /// </summary>
    All = Eureka | Identity | MongoDb | MySql | PostgreSql | RabbitMQ | Redis | SqlServer
}
