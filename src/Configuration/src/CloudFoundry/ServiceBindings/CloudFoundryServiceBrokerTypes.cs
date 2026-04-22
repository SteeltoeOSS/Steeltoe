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
    None = 0x0,

    Eureka = 0x1,
    Identity = 0x2,
    MongoDb = 0x4,
    MySql = 0x8,
    PostgreSql = 0x10,
    RabbitMQ = 0x20,
    Redis = 0x40,
    SqlServer = 0x80,

    All = Eureka | Identity | MongoDb | MySql | PostgreSql | RabbitMQ | Redis | SqlServer
}
