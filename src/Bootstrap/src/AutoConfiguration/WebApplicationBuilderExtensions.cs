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
    /// <param name="webApplicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    /// <param name="exclusions">
    /// A list of assemblies to exclude from auto-configuration. For ease of use, select from <see cref="SteeltoeAssemblies" />.
    /// </param>
    /// <param name="loggerFactory">
    /// For logging within auto-configuration.
    /// </param>
    public static WebApplicationBuilder AddSteeltoe(this WebApplicationBuilder webApplicationBuilder, IEnumerable<string> exclusions = null,
        ILoggerFactory loggerFactory = null)
    {
        AssemblyExtensions.ExcludedAssemblies = exclusions ?? new List<string>();
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger(LoggerName);

        if (!webApplicationBuilder.WireIfLoaded(WireConfigServer, SteeltoeAssemblies.SteeltoeConfigurationConfigServer))
        {
            webApplicationBuilder.WireIfLoaded(WireCloudFoundryConfiguration, SteeltoeAssemblies.SteeltoeConfigurationCloudFoundry);
        }

        if (Platform.IsKubernetes && AssemblyExtensions.IsAssemblyLoaded(SteeltoeAssemblies.SteeltoeConfigurationKubernetes))
        {
            WireKubernetesConfiguration(webApplicationBuilder);
        }

        webApplicationBuilder.WireIfLoaded(WireRandomValueProvider, SteeltoeAssemblies.SteeltoeConfigurationRandomValue);

        webApplicationBuilder.WireIfLoaded(WirePlaceholderResolver, SteeltoeAssemblies.SteeltoeConfigurationPlaceholder);

        if (webApplicationBuilder.WireIfLoaded(WireConnectorConfiguration, SteeltoeAssemblies.SteeltoeConnectors))
        {
            webApplicationBuilder.WireIfAnyLoaded(WireCosmosDbConnector, CosmosDbPackageResolver.Default);
            webApplicationBuilder.WireIfAnyLoaded(WireMongoDbConnector, MongoDbPackageResolver.Default);
            webApplicationBuilder.WireIfAnyLoaded(WireMySqlConnector, MySqlPackageResolver.Default);
            webApplicationBuilder.WireIfAnyLoaded(WirePostgreSqlConnector, PostgreSqlPackageResolver.Default);
            webApplicationBuilder.WireIfAnyLoaded(WireRabbitMQConnector, RabbitMQPackageResolver.Default);
            webApplicationBuilder.WireIfAnyLoaded(WireRedisConnector, StackExchangeRedisPackageResolver.Default, MicrosoftRedisPackageResolver.Default);
            webApplicationBuilder.WireIfAnyLoaded(WireSqlServerConnector, SqlServerPackageResolver.Default);
        }

        webApplicationBuilder.WireIfLoaded(WireDynamicSerilog, SteeltoeAssemblies.SteeltoeLoggingDynamicSerilog);

        webApplicationBuilder.WireIfLoaded(WireDiscoveryClient, SteeltoeAssemblies.SteeltoeDiscoveryClient);

        if (AssemblyExtensions.IsAssemblyLoaded(SteeltoeAssemblies.SteeltoeManagementKubernetes))
        {
            webApplicationBuilder.WireIfLoaded(WireKubernetesActuators, SteeltoeAssemblies.SteeltoeManagementKubernetes);
        }
        else
        {
            webApplicationBuilder.WireIfLoaded(WireAllActuators, SteeltoeAssemblies.SteeltoeManagementEndpoint);
        }

        webApplicationBuilder.WireIfLoaded(WireWavefrontMetrics, SteeltoeAssemblies.SteeltoeWavefront);

        webApplicationBuilder.WireIfLoaded(WireDistributedTracing, SteeltoeAssemblies.SteeltoeManagementTracing);

        webApplicationBuilder.WireIfLoaded(WireCloudFoundryContainerIdentity, SteeltoeAssemblies.SteeltoeSecurityAuthenticationCloudFoundry);
        return webApplicationBuilder;
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

    private static void WireIfAnyLoaded(this WebApplicationBuilder webApplicationBuilder, Action<WebApplicationBuilder> action,
        params PackageResolver[] packageResolvers)
    {
        if (packageResolvers.Any(packageResolver => packageResolver.IsAvailable()))
        {
            action(webApplicationBuilder);
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
        Log(LogMessages.WireConfigServer);
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
        Log(LogMessages.WireRandomValueProvider);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WirePlaceholderResolver(this WebApplicationBuilder webApplicationBuilder)
    {
        ((IConfigurationBuilder)webApplicationBuilder.Configuration).AddPlaceholderResolver(_loggerFactory);
        Log(LogMessages.WirePlaceholderResolver);
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
        Log(LogMessages.WireMySqlConnection);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireCosmosDbConnector(this WebApplicationBuilder builder)
    {
        builder.AddCosmosDb();
        Log(LogMessages.WireCosmosClient);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireMongoDbConnector(this WebApplicationBuilder builder)
    {
        builder.AddMongoDb();
        Log(LogMessages.WireMongoClient);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WirePostgreSqlConnector(this WebApplicationBuilder builder)
    {
        builder.AddPostgreSql();
        Log(LogMessages.WirePostgreSqlConnection);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireRabbitMQConnector(this WebApplicationBuilder builder)
    {
        builder.AddRabbitMQ();
        Log(LogMessages.WireRabbitMQConnection);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireRedisConnector(this WebApplicationBuilder builder)
    {
        builder.AddRedis();
        Log(LogMessages.WireRedisConnectionMultiplexer);

        if (MicrosoftRedisPackageResolver.Default.IsAvailable())
        {
            Log(LogMessages.WireRedisDistributedCache);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireSqlServerConnector(this WebApplicationBuilder builder)
    {
        builder.AddSqlServer();
        Log(LogMessages.WireSqlServerConnection);
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
        if (!webApplicationBuilder.Configuration.HasWavefront())
        {
            return;
        }

        webApplicationBuilder.AddWavefrontMetrics();
        Log(LogMessages.WireWavefrontMetrics);
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
