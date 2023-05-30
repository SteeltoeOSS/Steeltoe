// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding.PostProcessors;
using Steeltoe.Configuration.Kubernetes.ServiceBinding;
using Steeltoe.Configuration.Kubernetes.ServiceBinding.PostProcessors;
using Steeltoe.Connectors.Redis.RuntimeTypeAccess;
using Steeltoe.Connectors.RuntimeTypeAccess;

namespace Steeltoe.Connectors.Redis;

public static class RedisServiceCollectionExtensions
{
    public static IServiceCollection AddRedis(this IServiceCollection services, IConfigurationBuilder configurationBuilder)
    {
        return AddRedis(services, configurationBuilder, null);
    }

    public static IServiceCollection AddRedis(this IServiceCollection services, IConfigurationBuilder configurationBuilder,
        Action<ConnectorSetupOptions>? setupAction)
    {
        return AddRedis(services, configurationBuilder, new StackExchangeRedisPackageResolver(), new MicrosoftRedisPackageResolver(), setupAction);
    }

    internal static IServiceCollection AddRedis(this IServiceCollection services, IConfigurationBuilder configurationBuilder,
        StackExchangeRedisPackageResolver stackExchangeRedisPackageResolver, MicrosoftRedisPackageResolver microsoftRedisPackageResolver,
        Action<ConnectorSetupOptions>? setupAction)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configurationBuilder);
        ArgumentGuard.NotNull(stackExchangeRedisPackageResolver);
        ArgumentGuard.NotNull(microsoftRedisPackageResolver);

        var setupOptions = new ConnectorSetupOptions();
        setupAction?.Invoke(setupOptions);

        RegisterPostProcessors(configurationBuilder);

        ConnectorCreateHealthContributor? createHealthContributor = setupOptions.EnableHealthChecks
            ? (serviceProvider, serviceBindingName) => setupOptions.CreateHealthContributor != null
                ? setupOptions.CreateHealthContributor(serviceProvider, serviceBindingName)
                : CreateHealthContributor(serviceProvider, serviceBindingName, stackExchangeRedisPackageResolver)
            : null;

        IReadOnlySet<string> optionNames =
            ConnectorOptionsBinder.RegisterNamedOptions<RedisOptions>(services, configurationBuilder, "redis", createHealthContributor);

        ConnectorCreateConnection stackExchangeCreateConnection = (serviceProvider, serviceBindingName) => setupOptions.CreateConnection != null
            ? setupOptions.CreateConnection(serviceProvider, serviceBindingName)
            : CreateConnectionMultiplexer(serviceProvider, serviceBindingName, stackExchangeRedisPackageResolver);

        ConnectorFactoryShim<RedisOptions>.Register(stackExchangeRedisPackageResolver.ConnectionMultiplexerInterface.Type, services, optionNames,
            stackExchangeCreateConnection, true);

        ConnectorCreateConnection microsoftCreateConnection = (serviceProvider, serviceBindingName) => CreateDistributedCache(stackExchangeCreateConnection,
            serviceProvider, serviceBindingName, stackExchangeRedisPackageResolver, microsoftRedisPackageResolver);

        ConnectorFactoryShim<RedisOptions>.Register(microsoftRedisPackageResolver.DistributedCacheInterface.Type, services, optionNames,
            microsoftCreateConnection, true);

        return services;
    }

    private static void RegisterPostProcessors(IConfigurationBuilder builder)
    {
        builder.AddCloudFoundryServiceBindings();
        CloudFoundryServiceBindingConfigurationSource cloudFoundrySource = builder.Sources.OfType<CloudFoundryServiceBindingConfigurationSource>().First();
        cloudFoundrySource.RegisterPostProcessor(new RedisCloudFoundryPostProcessor());

        builder.AddKubernetesServiceBindings();
        KubernetesServiceBindingConfigurationSource kubernetesSource = builder.Sources.OfType<KubernetesServiceBindingConfigurationSource>().First();
        kubernetesSource.RegisterPostProcessor(new RedisKubernetesPostProcessor());

        var connectionStringPostProcessor = new RedisConnectionStringPostProcessor();
        var connectionStringSource = new ConnectionStringPostProcessorConfigurationSource();
        connectionStringSource.RegisterPostProcessor(connectionStringPostProcessor);
        builder.Add(connectionStringSource);
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

    private static IDisposable CreateConnectionMultiplexer(IServiceProvider serviceProvider, string serviceBindingName,
        StackExchangeRedisPackageResolver packageResolver)
    {
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<RedisOptions>>();
        RedisOptions options = optionsMonitor.Get(serviceBindingName);

        ConnectionMultiplexerShim connectionMultiplexerShim = ConnectionMultiplexerShim.Connect(packageResolver, options.ConnectionString!);
        return connectionMultiplexerShim.Instance;
    }

    private static object CreateDistributedCache(ConnectorCreateConnection stackExchangeCreateConnection, IServiceProvider serviceProvider,
        string serviceBindingName, StackExchangeRedisPackageResolver stackExchangeRedisPackageResolver,
        MicrosoftRedisPackageResolver microsoftRedisPackageResolver)
    {
        object connectionMultiplexerInstance = stackExchangeCreateConnection(serviceProvider, serviceBindingName);
        var connectionMultiplexerShim = new ConnectionMultiplexerInterfaceShim(stackExchangeRedisPackageResolver, connectionMultiplexerInstance);

        var redisCacheOptionsShim = RedisCacheOptionsShim.CreateInstance(microsoftRedisPackageResolver);
        redisCacheOptionsShim.InstanceName = connectionMultiplexerShim.ClientName;
        redisCacheOptionsShim.ConnectionMultiplexerFactory = redisCacheOptionsShim.CreateTaskLambdaForConnectionMultiplexerFactory(connectionMultiplexerShim);

        var redisCacheShim = RedisCacheShim.CreateInstance(microsoftRedisPackageResolver, redisCacheOptionsShim);
        return redisCacheShim.Instance;
    }
}
