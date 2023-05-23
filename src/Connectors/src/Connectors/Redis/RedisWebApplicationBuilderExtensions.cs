// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.Redis.RuntimeTypeAccess;
using Steeltoe.Connectors.RuntimeTypeAccess;

namespace Steeltoe.Connectors.Redis;

public delegate IDisposable CreateConnectionMultiplexer(RedisOptions options, string serviceBindingName);

public static class RedisWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddRedis(this WebApplicationBuilder builder, CreateConnectionMultiplexer? createConnectionMultiplexer = null)
    {
        return AddRedis(builder, new StackExchangeRedisPackageResolver(), new MicrosoftRedisPackageResolver(), createConnectionMultiplexer);
    }

    private static WebApplicationBuilder AddRedis(this WebApplicationBuilder builder, StackExchangeRedisPackageResolver stackExchangeRedisPackageResolver,
        MicrosoftRedisPackageResolver microsoftRedisPackageResolver, CreateConnectionMultiplexer? createConnectionMultiplexer)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(stackExchangeRedisPackageResolver);
        ArgumentGuard.NotNull(microsoftRedisPackageResolver);

        var connectionStringPostProcessor = new RedisConnectionStringPostProcessor();
        BaseWebApplicationBuilderExtensions.RegisterConfigurationSource(builder.Configuration, connectionStringPostProcessor);

        Func<IServiceProvider, string, IHealthContributor> createHealthContributor = (serviceProvider, serviceBindingName) =>
            CreateHealthContributor(serviceProvider, serviceBindingName, stackExchangeRedisPackageResolver);

        BaseWebApplicationBuilderExtensions.RegisterNamedOptions<RedisOptions>(builder, "redis", createHealthContributor);

        Func<RedisOptions, string, object> stackExchangeCreateConnection = (options, serviceBindingName) => createConnectionMultiplexer != null
            ? createConnectionMultiplexer(options, serviceBindingName)
            : ConnectionMultiplexerShim.Connect(stackExchangeRedisPackageResolver, options.ConnectionString!).Instance;

        ConnectorFactoryShim<RedisOptions>.Register(builder.Services, stackExchangeRedisPackageResolver.ConnectionMultiplexerInterface.Type, true,
            stackExchangeCreateConnection);

        Func<RedisOptions, string, object> microsoftCreateConnection = (options, serviceBindingName) => CreateMicrosoftConnection(stackExchangeCreateConnection,
            options, serviceBindingName, stackExchangeRedisPackageResolver, microsoftRedisPackageResolver);

        ConnectorFactoryShim<RedisOptions>.Register(builder.Services, microsoftRedisPackageResolver.DistributedCacheInterface.Type, true,
            microsoftCreateConnection);

        return builder;
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string serviceBindingName,
        StackExchangeRedisPackageResolver packageResolver)
    {
        ConnectorFactoryShim<RedisOptions> connectorFactoryShim =
            ConnectorFactoryShim<RedisOptions>.FromServiceProvider(serviceProvider, packageResolver.ConnectionMultiplexerInterface.Type);

        ConnectorShim<RedisOptions> connectorShim = connectorFactoryShim.GetNamed(serviceBindingName);

        object redisClient = connectorShim.GetConnection();
        string hostName = GetHostNameFromConnectionString(connectorShim.Options.ConnectionString);
        var logger = serviceProvider.GetRequiredService<ILogger<RedisHealthContributor>>();

        return new RedisHealthContributor(redisClient, $"Redis-{serviceBindingName}", hostName, logger);
    }

    private static string GetHostNameFromConnectionString(string? connectionString)
    {
        if (connectionString == null)
        {
            return string.Empty;
        }

        var builder = new RedisConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        return (string)builder["host"]!;
    }

    private static object CreateMicrosoftConnection(Func<RedisOptions, string, object> nativeCreateConnection, RedisOptions options, string serviceBindingName,
        StackExchangeRedisPackageResolver stackExchangeRedisPackageResolver, MicrosoftRedisPackageResolver microsoftRedisPackageResolver)
    {
        object connectionMultiplexerInstance = nativeCreateConnection(options, serviceBindingName);
        var connectionMultiplexerShim = new ConnectionMultiplexerInterfaceShim(stackExchangeRedisPackageResolver, connectionMultiplexerInstance);

        var redisCacheOptionsShim = RedisCacheOptionsShim.CreateInstance(microsoftRedisPackageResolver);
        redisCacheOptionsShim.InstanceName = connectionMultiplexerShim.ClientName;
        redisCacheOptionsShim.ConnectionMultiplexerFactory = redisCacheOptionsShim.CreateTaskLambdaForConnectionMultiplexerFactory(connectionMultiplexerShim);

        var redisCacheShim = RedisCacheShim.CreateInstance(microsoftRedisPackageResolver, redisCacheOptionsShim);
        return redisCacheShim.Instance;
    }
}
