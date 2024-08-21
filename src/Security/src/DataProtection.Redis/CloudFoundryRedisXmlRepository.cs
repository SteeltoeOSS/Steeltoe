// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.DataProtection.StackExchangeRedis;
using StackExchange.Redis;
using Steeltoe.Connectors;
using Steeltoe.Connectors.Redis;

namespace Steeltoe.Security.DataProtection.Redis;

internal sealed class CloudFoundryRedisXmlRepository(ConnectorFactory<RedisOptions, IConnectionMultiplexer> connectorFactory)
    : RedisXmlRepository(() => GetDatabase(connectorFactory), DataProtectionKey)
{
    private static readonly RedisKey DataProtectionKey = "Steeltoe-DataProtection-Key";

    private static IDatabase GetDatabase(ConnectorFactory<RedisOptions, IConnectionMultiplexer> connectorFactory)
    {
        ArgumentNullException.ThrowIfNull(connectorFactory);

        Connector<RedisOptions, IConnectionMultiplexer> connector = connectorFactory.Get();
        IConnectionMultiplexer connectionMultiplexer = connector.GetConnection();
        return connectionMultiplexer.GetDatabase();
    }
}
