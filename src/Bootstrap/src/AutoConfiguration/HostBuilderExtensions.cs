// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common;
using Steeltoe.Common.DynamicTypeAccess;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Configuration.ConfigServer;
using Steeltoe.Configuration.Kubernetes;
using Steeltoe.Configuration.Placeholder;
using Steeltoe.Configuration.RandomValue;
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
using Steeltoe.Management.Prometheus;
using Steeltoe.Management.Tracing;
using Steeltoe.Management.Wavefront;
using Steeltoe.Security.Authentication.CloudFoundry;

namespace Steeltoe.Bootstrap.AutoConfiguration;

public static class HostBuilderExtensions
{
    private const string LoggerName = "Steeltoe.AutoConfiguration";
    private static ILoggerFactory _loggerFactory;

    static HostBuilderExtensions()
    {
        AppDomain currentDomain = AppDomain.CurrentDomain;
        currentDomain.AssemblyResolve += AssemblyExtensions.LoadAnyVersion;
    }

    /// <summary>
    /// Automatically configure Steeltoe packages that have been added as NuGet references.
    /// <para />
    /// PLEASE NOTE: No extensions to IApplicationBuilder will be configured.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="IHostBuilder" />.
    /// </param>
    /// <param name="exclusions">
    /// A list of assemblies to exclude from auto-configuration. For ease of use, select from <see cref="SteeltoeAssemblyNames" />.
    /// </param>
    /// <param name="loggerFactory">
    /// For logging within auto-configuration.
    /// </param>
    public static IHostBuilder AddSteeltoe(this IHostBuilder builder, IEnumerable<string> exclusions = null, ILoggerFactory loggerFactory = null)
    {
        AssemblyExtensions.ExcludedAssemblies = exclusions ?? new List<string>();
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        ILogger logger = _loggerFactory.CreateLogger(LoggerName);
        builder.Properties[LoggerName] = logger;

        if (!builder.WireIfLoaded(WireConfigServer, SteeltoeAssemblyNames.ConfigurationConfigServer))
        {
            builder.WireIfLoaded(WireCloudFoundryConfiguration, SteeltoeAssemblyNames.ConfigurationCloudFoundry);
        }

        if (Platform.IsKubernetes && AssemblyExtensions.IsAssemblyLoaded(SteeltoeAssemblyNames.ConfigurationKubernetes))
        {
            WireKubernetesConfiguration(builder);
        }

        builder.WireIfLoaded(WireRandomValueProvider, SteeltoeAssemblyNames.ConfigurationRandomValue);

        builder.WireIfLoaded(WirePlaceholderResolver, SteeltoeAssemblyNames.ConfigurationPlaceholder);

        builder.WireIfLoaded(WireConnectors, SteeltoeAssemblyNames.Connectors);

        builder.WireIfLoaded(WireDynamicSerilog, SteeltoeAssemblyNames.LoggingDynamicSerilog);
        builder.WireIfLoaded(WireDiscoveryClient, SteeltoeAssemblyNames.DiscoveryClient);

        if (AssemblyExtensions.IsAssemblyLoaded(SteeltoeAssemblyNames.ManagementKubernetes))
        {
            builder.WireIfLoaded(WireKubernetesActuators, SteeltoeAssemblyNames.ManagementKubernetes);
        }
        else
        {
            builder.WireIfLoaded(WireAllActuators, SteeltoeAssemblyNames.ManagementEndpoint);
        }

        builder.WireIfLoaded(WireSteeltoePrometheus, SteeltoeAssemblyNames.ManagementPrometheus);

        builder.WireIfLoaded(WireWavefrontMetrics, SteeltoeAssemblyNames.ManagementWavefront);

        builder.WireIfLoaded(WireDistributedTracing, SteeltoeAssemblyNames.ManagementTracing);

        builder.WireIfLoaded(WireCloudFoundryContainerIdentity, SteeltoeAssemblyNames.SecurityAuthenticationCloudFoundry);
        return builder;
    }

    private static bool WireIfLoaded(this IHostBuilder hostBuilder, Action<IHostBuilder> action, params string[] assembly)
    {
        if (Array.TrueForAll(assembly, AssemblyExtensions.IsAssemblyLoaded))
        {
            action(hostBuilder);
            return true;
        }

        return false;
    }

    private static void WireIfAnyLoaded(this IHostBuilder hostBuilder, Action<IHostBuilder> action, IReadOnlySet<string> assemblyNamesToExclude,
        params PackageResolver[] packageResolvers)
    {
        if (Array.Exists(packageResolvers, packageResolver => packageResolver.IsAvailable(assemblyNamesToExclude)))
        {
            action(hostBuilder);
        }
    }

    private static void Log(this IHostBuilder host, string message)
    {
        var logger = (ILogger)host.Properties[LoggerName];
        logger.LogInformation(message);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireConfigServer(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration((context, cfg) => cfg.AddConfigServer(context.HostingEnvironment, _loggerFactory))
            .ConfigureServices((_, services) => services.AddConfigServerServices()).Log(LogMessages.WireConfigServerConfiguration);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireCloudFoundryConfiguration(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(cfg => cfg.AddCloudFoundry()).Log(LogMessages.WireCloudFoundryConfiguration);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireKubernetesConfiguration(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(cfg => cfg.AddKubernetes(_loggerFactory))
            .ConfigureServices(serviceCollection => serviceCollection.AddKubernetesConfigurationServices()).Log(LogMessages.WireKubernetesConfiguration);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireRandomValueProvider(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(cfg => cfg.AddRandomValueSource(_loggerFactory)).Log(LogMessages.WireRandomValueConfiguration);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WirePlaceholderResolver(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(cfg => cfg.AddPlaceholderResolver(_loggerFactory)).Log(LogMessages.WirePlaceholderConfiguration);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireConnectors(IHostBuilder builder)
    {
        var assemblyNamesToExclude = new HashSet<string>(AssemblyExtensions.ExcludedAssemblies, StringComparer.OrdinalIgnoreCase);

        builder.WireIfAnyLoaded(WireCosmosDbConnector, assemblyNamesToExclude, CosmosDbPackageResolver.Default);
        builder.WireIfAnyLoaded(WireMongoDbConnector, assemblyNamesToExclude, MongoDbPackageResolver.Default);
        builder.WireIfAnyLoaded(WireMySqlConnector, assemblyNamesToExclude, MySqlPackageResolver.Default);
        builder.WireIfAnyLoaded(WirePostgreSqlConnector, assemblyNamesToExclude, PostgreSqlPackageResolver.Default);
        builder.WireIfAnyLoaded(WireRabbitMQConnector, assemblyNamesToExclude, RabbitMQPackageResolver.Default);
        builder.WireIfAnyLoaded(WireRedisConnector, assemblyNamesToExclude, StackExchangeRedisPackageResolver.Default, MicrosoftRedisPackageResolver.Default);
        builder.WireIfAnyLoaded(WireSqlServerConnector, assemblyNamesToExclude, SqlServerPackageResolver.Default);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireMySqlConnector(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.ConfigureMySql();
        });

        hostBuilder.ConfigureServices((host, services) =>
        {
            services.AddMySql(host.Configuration);
        });

        hostBuilder.Log(LogMessages.WireMySqlConnector);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireCosmosDbConnector(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.ConfigureCosmosDb();
        });

        hostBuilder.ConfigureServices((host, services) =>
        {
            services.AddCosmosDb(host.Configuration);
        });

        hostBuilder.Log(LogMessages.WireCosmosDbConnector);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireMongoDbConnector(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.ConfigureMongoDb();
        });

        hostBuilder.ConfigureServices((host, services) =>
        {
            services.AddMongoDb(host.Configuration);
        });

        hostBuilder.Log(LogMessages.WireMongoDbConnector);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WirePostgreSqlConnector(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.ConfigurePostgreSql();
        });

        hostBuilder.ConfigureServices((host, services) =>
        {
            services.AddPostgreSql(host.Configuration);
        });

        hostBuilder.Log(LogMessages.WirePostgreSqlConnector);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireRabbitMQConnector(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.ConfigureRabbitMQ();
        });

        hostBuilder.ConfigureServices((host, services) =>
        {
            services.AddRabbitMQ(host.Configuration);
        });

        hostBuilder.Log(LogMessages.WireRabbitMQConnector);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireRedisConnector(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.ConfigureRedis();
        });

        hostBuilder.ConfigureServices((host, services) =>
        {
            services.AddRedis(host.Configuration);
        });

        hostBuilder.Log(LogMessages.WireStackExchangeRedisConnector);

        // Intentionally ignoring excluded assemblies here.
        if (MicrosoftRedisPackageResolver.Default.IsAvailable())
        {
            hostBuilder.Log(LogMessages.WireDistributedCacheRedisConnector);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireSqlServerConnector(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.ConfigureSqlServer();
        });

        hostBuilder.ConfigureServices((host, services) =>
        {
            services.AddSqlServer(host.Configuration);
        });

        hostBuilder.Log(LogMessages.WireSqlServerConnector);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireDiscoveryClient(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((host, svc) => svc.AddDiscoveryClient(host.Configuration)).Log(LogMessages.WireDiscoveryClient);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireDistributedTracing(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((_, svc) => svc.AddDistributedTracingAspNetCore()).Log(LogMessages.WireDistributedTracing);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireKubernetesActuators(this IHostBuilder hostBuilder)
    {
        hostBuilder.AddKubernetesActuators().Log(LogMessages.WireKubernetesActuators);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireAllActuators(this IHostBuilder hostBuilder)
    {
        hostBuilder.AddAllActuators().Log(LogMessages.WireAllActuators);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireWavefrontMetrics(this IHostBuilder hostBuilder)
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
    private static void WireSteeltoePrometheus(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((_, collection) => collection.AddPrometheusActuator()).Log(LogMessages.WirePrometheus);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireDynamicSerilog(this IHostBuilder hostBuilder)
    {
        hostBuilder.AddDynamicSerilog().Log(LogMessages.WireDynamicSerilog);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireCloudFoundryContainerIdentity(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(cfg => cfg.AddCloudFoundryContainerIdentity()).ConfigureServices((_, svc) => svc.AddCloudFoundryCertificateAuth())
            .Log(LogMessages.WireCloudFoundryContainerIdentity);
    }
}
