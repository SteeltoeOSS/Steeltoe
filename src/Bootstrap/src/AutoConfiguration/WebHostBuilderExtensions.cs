// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#pragma warning disable 0436 // Type conflicts with imported type

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common;
using Steeltoe.Common.DynamicTypeAccess;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Configuration.ConfigServer;
using Steeltoe.Configuration.Kubernetes;
using Steeltoe.Configuration.Placeholder;
using Steeltoe.Configuration.RandomValue;
using Steeltoe.Connectors;
using Steeltoe.Connectors.CosmosDb;
using Steeltoe.Connectors.CosmosDb.DynamicTypeAccess;
using Steeltoe.Connectors.MongoDb;
using Steeltoe.Connectors.MongoDb.DynamicTypeAccess;
using Steeltoe.Connectors.MySql;
using Steeltoe.Connectors.MySql.DynamicTypeAccess;
using Steeltoe.Connectors.PostgreSql;
using Steeltoe.Connectors.PostgreSql.DynamicTypeAccess;
using Steeltoe.Connectors.RabbitMQ;
using Steeltoe.Connectors.RabbitMQ.DynamicTypeAccess;
using Steeltoe.Connectors.Redis;
using Steeltoe.Connectors.Redis.DynamicTypeAccess;
using Steeltoe.Connectors.SqlServer;
using Steeltoe.Connectors.SqlServer.RuntimeTypeAccess;
using Steeltoe.Discovery.Client;
using Steeltoe.Logging.DynamicSerilog;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Kubernetes;
using Steeltoe.Management.Tracing;
using Steeltoe.Management.Wavefront;
using Steeltoe.Security.Authentication.CloudFoundry;

namespace Steeltoe.Bootstrap.AutoConfiguration;

public static class WebHostBuilderExtensions
{
    private const string LoggerName = "Steeltoe.AutoConfiguration";
    private static ILoggerFactory _loggerFactory;
    private static ILogger _logger;

    static WebHostBuilderExtensions()
    {
        AppDomain currentDomain = AppDomain.CurrentDomain;
        currentDomain.AssemblyResolve += AssemblyExtensions.LoadAnyVersion;
    }

    /// <summary>
    /// Automatically configure Steeltoe packages that have been added as NuGet references.
    /// <para />
    /// PLEASE NOTE: No extensions to IApplicationBuilder will be configured.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your <see cref="IWebHostBuilder" />.
    /// </param>
    /// <param name="exclusions">
    /// A list of assemblies to exclude from auto-configuration. For ease of use, select from <see cref="SteeltoeAssemblies" />.
    /// </param>
    /// <param name="loggerFactory">
    /// For logging within auto-configuration.
    /// </param>
    public static IWebHostBuilder AddSteeltoe(this IWebHostBuilder hostBuilder, IEnumerable<string> exclusions = null, ILoggerFactory loggerFactory = null)
    {
        AssemblyExtensions.ExcludedAssemblies = exclusions ?? new List<string>();
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger(LoggerName);

        if (!hostBuilder.WireIfLoaded(WireConfigServer, SteeltoeAssemblies.SteeltoeConfigurationConfigServer))
        {
            hostBuilder.WireIfLoaded(WireCloudFoundryConfiguration, SteeltoeAssemblies.SteeltoeConfigurationCloudFoundry);
        }

        if (Platform.IsKubernetes && AssemblyExtensions.IsAssemblyLoaded(SteeltoeAssemblies.SteeltoeConfigurationKubernetes))
        {
            WireKubernetesConfiguration(hostBuilder);
        }

        hostBuilder.WireIfLoaded(WireRandomValueProvider, SteeltoeAssemblies.SteeltoeConfigurationRandomValue);

        hostBuilder.WireIfLoaded(WirePlaceholderResolver, SteeltoeAssemblies.SteeltoeConfigurationPlaceholder);

        if (hostBuilder.WireIfLoaded(WireConnectorConfiguration, SteeltoeAssemblies.SteeltoeConnectors))
        {
            hostBuilder.WireIfAnyLoaded(WireCosmosDbConnector, CosmosDbPackageResolver.Default);
            hostBuilder.WireIfAnyLoaded(WireMongoDbConnector, MongoDbPackageResolver.Default);
            hostBuilder.WireIfAnyLoaded(WireMySqlConnector, MySqlPackageResolver.Default);
            hostBuilder.WireIfAnyLoaded(WirePostgreSqlConnector, PostgreSqlPackageResolver.Default);
            hostBuilder.WireIfAnyLoaded(WireRabbitMQConnector, RabbitMQPackageResolver.Default);
            hostBuilder.WireIfAnyLoaded(WireRedisConnector, StackExchangeRedisPackageResolver.Default, MicrosoftRedisPackageResolver.Default);
            hostBuilder.WireIfAnyLoaded(WireSqlServerConnector, SqlServerPackageResolver.Default);
        }

        hostBuilder.WireIfLoaded(WireDynamicSerilog, SteeltoeAssemblies.SteeltoeLoggingDynamicSerilog);
        hostBuilder.WireIfLoaded(WireDiscoveryClient, SteeltoeAssemblies.SteeltoeDiscoveryClient);

        if (AssemblyExtensions.IsAssemblyLoaded(SteeltoeAssemblies.SteeltoeManagementKubernetes))
        {
            hostBuilder.WireIfLoaded(WireKubernetesActuators, SteeltoeAssemblies.SteeltoeManagementKubernetes);
        }
        else
        {
            hostBuilder.WireIfLoaded(WireAllActuators, SteeltoeAssemblies.SteeltoeManagementEndpoint);
        }

        hostBuilder.WireIfLoaded(WireWavefrontMetrics, SteeltoeAssemblies.SteeltoeWavefront);

        hostBuilder.WireIfLoaded(WireDistributedTracing, SteeltoeAssemblies.SteeltoeManagementTracing);

        hostBuilder.WireIfLoaded(WireCloudFoundryContainerIdentity, SteeltoeAssemblies.SteeltoeSecurityAuthenticationCloudFoundry);
        return hostBuilder;
    }

    private static bool WireIfLoaded(this IWebHostBuilder hostBuilder, Action<IWebHostBuilder> action, params string[] assembly)
    {
        if (assembly.All(AssemblyExtensions.IsAssemblyLoaded))
        {
            action(hostBuilder);
            return true;
        }

        return false;
    }

    private static void WireIfAnyLoaded(this IWebHostBuilder hostBuilder, Action<IWebHostBuilder> action, params PackageResolver[] packageResolvers)
    {
        if (packageResolvers.Any(packageResolver => packageResolver.IsAvailable()))
        {
            action(hostBuilder);
        }
    }

    private static IWebHostBuilder Log(this IWebHostBuilder host, string message)
    {
        _logger.LogInformation(message);
        return host;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireConfigServer(this IWebHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration((context, cfg) => cfg.AddConfigServer(context.HostingEnvironment, _loggerFactory))
            .ConfigureServices((_, services) => services.AddConfigServerServices()).Log(LogMessages.WireConfigServer);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireCloudFoundryConfiguration(this IWebHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(cfg => cfg.AddCloudFoundry()).Log(LogMessages.WireCloudFoundryConfiguration);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireKubernetesConfiguration(this IWebHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(cfg => cfg.AddKubernetes(_loggerFactory))
            .ConfigureServices(serviceCollection => serviceCollection.AddKubernetesConfigurationServices()).Log(LogMessages.WireKubernetesConfiguration);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireRandomValueProvider(this IWebHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(cfg => cfg.AddRandomValueSource(_loggerFactory)).Log(LogMessages.WireRandomValueProvider);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WirePlaceholderResolver(this IWebHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(cfg => cfg.AddPlaceholderResolver(_loggerFactory)).Log(LogMessages.WirePlaceholderResolver);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireConnectorConfiguration(this IWebHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration((_, svc) => svc.AddConnectionStrings()).Log(LogMessages.WireConnectorsConfiguration);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireMySqlConnector(this IWebHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.ConfigureMySql();
        });

        hostBuilder.ConfigureServices((host, services) =>
        {
            services.AddMySql(host.Configuration);
        });

        hostBuilder.Log(LogMessages.WireMySqlConnection);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireCosmosDbConnector(this IWebHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.ConfigureCosmosDb();
        });

        hostBuilder.ConfigureServices((host, services) =>
        {
            services.AddCosmosDb(host.Configuration);
        });

        hostBuilder.Log(LogMessages.WireCosmosClient);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireMongoDbConnector(this IWebHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.ConfigureMongoDb();
        });

        hostBuilder.ConfigureServices((host, services) =>
        {
            services.AddMongoDb(host.Configuration);
        });

        hostBuilder.Log(LogMessages.WireMongoClient);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WirePostgreSqlConnector(this IWebHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.ConfigurePostgreSql();
        });

        hostBuilder.ConfigureServices((host, services) =>
        {
            services.AddPostgreSql(host.Configuration);
        });

        hostBuilder.Log(LogMessages.WirePostgreSqlConnection);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireRabbitMQConnector(this IWebHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.ConfigureRabbitMQ();
        });

        hostBuilder.ConfigureServices((host, services) =>
        {
            services.AddRabbitMQ(host.Configuration);
        });

        hostBuilder.Log(LogMessages.WireRabbitMQConnection);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireRedisConnector(this IWebHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.ConfigureRedis();
        });

        hostBuilder.ConfigureServices((host, services) =>
        {
            services.AddRedis(host.Configuration);
        });

        hostBuilder.Log(LogMessages.WireRedisConnectionMultiplexer);

        if (MicrosoftRedisPackageResolver.Default.IsAvailable())
        {
            hostBuilder.Log(LogMessages.WireRedisDistributedCache);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireSqlServerConnector(this IWebHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.ConfigureSqlServer();
        });

        hostBuilder.ConfigureServices((host, services) =>
        {
            services.AddSqlServer(host.Configuration);
        });

        hostBuilder.Log(LogMessages.WireSqlServerConnection);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireDiscoveryClient(this IWebHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((_, svc) => svc.AddDiscoveryClient()).Log(LogMessages.WireDiscoveryClient);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireDistributedTracing(this IWebHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((_, svc) => svc.AddDistributedTracingAspNetCore()).Log(LogMessages.WireDistributedTracing);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireKubernetesActuators(this IWebHostBuilder hostBuilder)
    {
        hostBuilder.AddKubernetesActuators().Log(LogMessages.WireKubernetesActuators);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireAllActuators(this IWebHostBuilder hostBuilder)
    {
        hostBuilder.AddAllActuators().Log(LogMessages.WireAllActuators);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireWavefrontMetrics(this IWebHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((context, collection) =>
        {
            if (context.Configuration.HasWavefront())
            {
                collection.AddWavefrontMetrics();
                hostBuilder.Log(LogMessages.WireWavefrontMetrics);
            }
        });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireDynamicSerilog(this IWebHostBuilder hostBuilder)
    {
        hostBuilder.AddDynamicSerilog().Log(LogMessages.WireDynamicSerilog);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireCloudFoundryContainerIdentity(this IWebHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(cfg => cfg.AddCloudFoundryContainerIdentity()).ConfigureServices((_, svc) => svc.AddCloudFoundryCertificateAuth())
            .Log(LogMessages.WireCloudFoundryContainerIdentity);
    }
}
