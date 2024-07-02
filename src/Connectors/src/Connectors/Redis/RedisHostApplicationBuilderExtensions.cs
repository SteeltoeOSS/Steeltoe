// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;

namespace Steeltoe.Connectors.Redis;

public static class RedisHostApplicationBuilderExtensions
{
    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> (with type parameters <see cref="RedisOptions" /> and
    /// StackExchange.Redis.IConnectionMultiplexer) to connect to a Redis database. If Microsoft.Extensions.Caching.StackExchangeRedis is referenced, this
    /// method additionally registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> with type parameters <see cref="RedisOptions" /> and
    /// <see cref="IDistributedCache" />.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The <see cref="IHostApplicationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddRedis(this IHostApplicationBuilder builder)
    {
        return AddRedis(builder, null, null);
    }

    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> (with type parameters <see cref="RedisOptions" /> and
    /// StackExchange.Redis.IConnectionMultiplexer) to connect to a Redis database. If Microsoft.Extensions.Caching.StackExchangeRedis is referenced, this
    /// method additionally registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> with type parameters <see cref="RedisOptions" /> and
    /// <see cref="IDistributedCache" />.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <param name="configureAction">
    /// An optional delegate to configure configuration of this connector.
    /// </param>
    /// <param name="addAction">
    /// An optional delegate to configure this connector.
    /// </param>
    /// <returns>
    /// The <see cref="IHostApplicationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddRedis(this IHostApplicationBuilder builder, Action<ConnectorConfigureOptionsBuilder>? configureAction,
        Action<ConnectorAddOptionsBuilder>? addAction)
    {
        ArgumentGuard.NotNull(builder);

        builder.Configuration.ConfigureRedis(configureAction);
        builder.Services.AddRedis(builder.Configuration, addAction);
        return builder;
    }
}
