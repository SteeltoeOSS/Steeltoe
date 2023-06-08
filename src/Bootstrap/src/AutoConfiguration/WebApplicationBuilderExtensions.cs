// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
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
using Steeltoe.Management.Prometheus;
using Steeltoe.Management.Tracing;
using Steeltoe.Management.Wavefront;
using Steeltoe.Security.Authentication.CloudFoundry;

namespace Steeltoe.Bootstrap.AutoConfiguration;

public static class WebApplicationBuilderExtensions
{
    private const string LoggerName = "Steeltoe.AutoConfiguration";
    private static ILoggerFactory _loggerFactory;
    private static ILogger _logger;

    static WebApplicationBuilderExtensions()
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
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    /// <param name="exclusions">
    /// A list of assemblies to exclude from auto-configuration. For ease of use, select from <see cref="SteeltoeAssemblyNames" />.
    /// </param>
    /// <param name="loggerFactory">
    /// For logging within auto-configuration.
    /// </param>
    public static WebApplicationBuilder AddSteeltoe(this WebApplicationBuilder builder, IEnumerable<string> exclusions = null,
        ILoggerFactory loggerFactory = null)
    {
        AssemblyExtensions.ExcludedAssemblies = exclusions ?? new List<string>();
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger(LoggerName);

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

        if (builder.WireIfLoaded(WireConnectorConfiguration, SteeltoeAssemblyNames.Connectors))
        {
            var assemblyNamesToExclude = new HashSet<string>(AssemblyExtensions.ExcludedAssemblies, StringComparer.OrdinalIgnoreCase);

            builder.WireIfAnyLoaded(WireCosmosDbConnector, assemblyNamesToExclude, CosmosDbPackageResolver.Default);
            builder.WireIfAnyLoaded(WireMongoDbConnector, assemblyNamesToExclude, MongoDbPackageResolver.Default);
            builder.WireIfAnyLoaded(WireMySqlConnector, assemblyNamesToExclude, MySqlPackageResolver.Default);
            builder.WireIfAnyLoaded(WirePostgreSqlConnector, assemblyNamesToExclude, PostgreSqlPackageResolver.Default);
            builder.WireIfAnyLoaded(WireRabbitMQConnector, assemblyNamesToExclude, RabbitMQPackageResolver.Default);

            builder.WireIfAnyLoaded(WireRedisConnector, assemblyNamesToExclude, StackExchangeRedisPackageResolver.Default,
                MicrosoftRedisPackageResolver.Default);

            builder.WireIfAnyLoaded(WireSqlServerConnector, assemblyNamesToExclude, SqlServerPackageResolver.Default);
        }

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

    private static bool WireIfLoaded(this WebApplicationBuilder webApplicationBuilder, Action<WebApplicationBuilder> action, params string[] assembly)
    {
        if (assembly.All(AssemblyExtensions.IsAssemblyLoaded))
        {
            action(webApplicationBuilder);
            return true;
        }

        return false;
    }

    private static void WireIfAnyLoaded(this WebApplicationBuilder builder, Action<WebApplicationBuilder> action, IReadOnlySet<string> assemblyNamesToExclude,
        params PackageResolver[] packageResolvers)
    {
        if (packageResolvers.Any(packageResolver => packageResolver.IsAvailable(assemblyNamesToExclude)))
        {
            action(builder);
        }
    }

    private static void Log(string message)
    {
        _logger.LogInformation(message);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireConfigServer(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Configuration.AddConfigServer(webApplicationBuilder.Environment.EnvironmentName, _loggerFactory);
        webApplicationBuilder.Services.AddConfigServerServices();
        Log(LogMessages.WireConfigServerConfiguration);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireCloudFoundryConfiguration(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Configuration.AddCloudFoundry();
        Log(LogMessages.WireCloudFoundryConfiguration);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireKubernetesConfiguration(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Configuration.AddKubernetes(_loggerFactory);
        webApplicationBuilder.Services.AddKubernetesConfigurationServices();
        Log(LogMessages.WireKubernetesConfiguration);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireRandomValueProvider(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Configuration.AddRandomValueSource(_loggerFactory);
        Log(LogMessages.WireRandomValueConfiguration);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WirePlaceholderResolver(this WebApplicationBuilder webApplicationBuilder)
    {
        ((IConfigurationBuilder)webApplicationBuilder.Configuration).AddPlaceholderResolver(_loggerFactory);
        Log(LogMessages.WirePlaceholderConfiguration);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireConnectorConfiguration(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Configuration.AddConnectionStrings();
        Log(LogMessages.WireConnectorsConfiguration);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireMySqlConnector(this WebApplicationBuilder builder)
    {
        builder.AddMySql();
        Log(LogMessages.WireMySqlConnector);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireCosmosDbConnector(this WebApplicationBuilder builder)
    {
        builder.AddCosmosDb();
        Log(LogMessages.WireCosmosDbConnector);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireMongoDbConnector(this WebApplicationBuilder builder)
    {
        builder.AddMongoDb();
        Log(LogMessages.WireMongoDbConnector);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WirePostgreSqlConnector(this WebApplicationBuilder builder)
    {
        builder.AddPostgreSql();
        Log(LogMessages.WirePostgreSqlConnector);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireRabbitMQConnector(this WebApplicationBuilder builder)
    {
        builder.AddRabbitMQ();
        Log(LogMessages.WireRabbitMQConnector);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireRedisConnector(this WebApplicationBuilder builder)
    {
        builder.AddRedis();
        Log(LogMessages.WireStackExchangeRedisConnector);

        // Intentionally ignoring excluded assemblies here.
        if (MicrosoftRedisPackageResolver.Default.IsAvailable())
        {
            Log(LogMessages.WireDistributedCacheRedisConnector);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireSqlServerConnector(this WebApplicationBuilder builder)
    {
        builder.AddSqlServer();
        Log(LogMessages.WireSqlServerConnector);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireDiscoveryClient(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Services.AddDiscoveryClient(webApplicationBuilder.Configuration);
        Log(LogMessages.WireDiscoveryClient);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireDistributedTracing(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Services.AddDistributedTracingAspNetCore();
        Log(LogMessages.WireDistributedTracing);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireKubernetesActuators(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Services.AddKubernetesActuators(webApplicationBuilder.Configuration);
        webApplicationBuilder.Services.ActivateActuatorEndpoints();
        Log(LogMessages.WireKubernetesActuators);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireAllActuators(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Services.AddAllActuators();
        webApplicationBuilder.Services.ActivateActuatorEndpoints();
        Log(LogMessages.WireAllActuators);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireWavefrontMetrics(this WebApplicationBuilder webApplicationBuilder)
    {
        if (webApplicationBuilder.Configuration.HasWavefront())
        {
            webApplicationBuilder.AddWavefrontMetrics();
            Log(LogMessages.WireWavefrontMetrics);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireSteeltoePrometheus(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Services.AddPrometheusActuator();
        Log(LogMessages.WirePrometheus);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireDynamicSerilog(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Logging.AddDynamicSerilog();
        Log(LogMessages.WireDynamicSerilog);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireCloudFoundryContainerIdentity(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Configuration.AddCloudFoundryContainerIdentity();
        webApplicationBuilder.Services.AddCloudFoundryCertificateAuth();
        Log(LogMessages.WireCloudFoundryContainerIdentity);
    }
}
