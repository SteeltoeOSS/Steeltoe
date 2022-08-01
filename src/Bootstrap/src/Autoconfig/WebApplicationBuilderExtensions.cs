// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common;
using Steeltoe.Connector;
using Steeltoe.Connector.MongoDb;
using Steeltoe.Connector.MySql;
using Steeltoe.Connector.Oracle;
using Steeltoe.Connector.PostgreSql;
using Steeltoe.Connector.RabbitMQ;
using Steeltoe.Connector.Redis;
using Steeltoe.Connector.SqlServer;
using Steeltoe.Discovery.Client;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Steeltoe.Extensions.Configuration.ConfigServer;
using Steeltoe.Extensions.Configuration.Kubernetes;
using Steeltoe.Extensions.Configuration.Placeholder;
using Steeltoe.Extensions.Configuration.RandomValue;
using Steeltoe.Extensions.Logging.DynamicSerilog;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Kubernetes;
using Steeltoe.Management.Tracing;
using Steeltoe.Security.Authentication.CloudFoundry;
using System.Runtime.CompilerServices;

namespace Steeltoe.Bootstrap.Autoconfig;

public static class WebApplicationBuilderExtensions
{
    private const string LoggerName = "Steeltoe.Autoconfig";
    private static ILoggerFactory _loggerFactory;
    private static ILogger _logger;

    static WebApplicationBuilderExtensions()
    {
        var currentDomain = AppDomain.CurrentDomain;
        currentDomain.AssemblyResolve += AssemblyExtensions.LoadAnyVersion;
    }

    /// <summary>
    /// Automatically configure Steeltoe packages that have been added as NuGet references.<para />
    /// PLEASE NOTE: No extensions to IApplicationBuilder will be configured.
    /// </summary>
    /// <param name="webApplicationBuilder">Your <see cref="WebApplicationBuilder" />.</param>
    /// <param name="exclusions">A list of assemblies to exclude from auto-configuration. For ease of use, select from <see cref="SteeltoeAssemblies" />.</param>
    /// <param name="loggerFactory">For logging within auto-configuration.</param>
    public static WebApplicationBuilder AddSteeltoe(this WebApplicationBuilder webApplicationBuilder, IEnumerable<string> exclusions = null, ILoggerFactory loggerFactory = null)
    {
        AssemblyExtensions.ExcludedAssemblies = exclusions ?? new List<string>();
        _loggerFactory = loggerFactory;
        _logger = loggerFactory?.CreateLogger(LoggerName) ?? NullLogger.Instance;

        if (!webApplicationBuilder.WireIfAnyLoaded(WireConfigServer, SteeltoeAssemblies.SteeltoeExtensionsConfigurationConfigServerBase, SteeltoeAssemblies.SteeltoeExtensionsConfigurationConfigServerCore))
        {
            webApplicationBuilder.WireIfAnyLoaded(WireCloudFoundryConfiguration, SteeltoeAssemblies.SteeltoeExtensionsConfigurationCloudFoundryBase, SteeltoeAssemblies.SteeltoeExtensionsConfigurationCloudFoundryCore);
        }

        if (Platform.IsKubernetes && AssemblyExtensions.IsEitherAssemblyLoaded(SteeltoeAssemblies.SteeltoeExtensionsConfigurationKubernetesBase, SteeltoeAssemblies.SteeltoeExtensionsConfigurationKubernetesCore))
        {
            WireKubernetesConfiguration(webApplicationBuilder);
        }

        webApplicationBuilder.WireIfLoaded(WireRandomValueProvider, SteeltoeAssemblies.SteeltoeExtensionsConfigurationRandomValueBase);
        webApplicationBuilder.WireIfAnyLoaded(WirePlaceholderResolver, SteeltoeAssemblies.SteeltoeExtensionsConfigurationPlaceholderBase, SteeltoeAssemblies.SteeltoeExtensionsConfigurationPlaceholderCore);

        if (webApplicationBuilder.WireIfLoaded(WireConnectorConfiguration, SteeltoeAssemblies.SteeltoeConnectorConnectorCore))
        {
#pragma warning disable CS0436 // Type conflicts with imported type
            webApplicationBuilder.WireIfAnyLoaded(WireMySqlConnection, MySqlTypeLocator.Assemblies);
            webApplicationBuilder.WireIfAnyLoaded(WireMongoClient, MongoDbTypeLocator.Assemblies);
            webApplicationBuilder.WireIfAnyLoaded(WireOracleConnection, OracleTypeLocator.Assemblies);
            webApplicationBuilder.WireIfAnyLoaded(WirePostgresConnection, PostgreSqlTypeLocator.Assemblies);
            webApplicationBuilder.WireIfAnyLoaded(WireRabbitMqConnection, RabbitMQTypeLocator.Assemblies);
            webApplicationBuilder.WireIfAnyLoaded(WireRedisConnectionMultiplexer, RedisTypeLocator.StackExchangeAssemblies);
            webApplicationBuilder.WireIfAnyLoaded(WireDistributedRedisCache, RedisTypeLocator.MicrosoftAssemblies.Except(new[] { "Microsoft.Extensions.Caching.Abstractions" }).ToArray());
            webApplicationBuilder.WireIfAnyLoaded(WireSqlServerConnection, SqlServerTypeLocator.Assemblies);
#pragma warning restore CS0436 // Type conflicts with imported type
        }

        webApplicationBuilder.WireIfLoaded(WireDynamicSerilog, SteeltoeAssemblies.SteeltoeExtensionsLoggingDynamicSerilogCore);
        webApplicationBuilder.WireIfAnyLoaded(WireDiscoveryClient, SteeltoeAssemblies.SteeltoeDiscoveryClientBase, SteeltoeAssemblies.SteeltoeDiscoveryClientCore);

        if (AssemblyExtensions.IsAssemblyLoaded(SteeltoeAssemblies.SteeltoeManagementKubernetesCore))
        {
            webApplicationBuilder.WireIfLoaded(WireKubernetesActuators, SteeltoeAssemblies.SteeltoeManagementKubernetesCore);
        }
        else
        {
            webApplicationBuilder.WireIfLoaded(WireAllActuators, SteeltoeAssemblies.SteeltoeManagementEndpointCore);
        }

        webApplicationBuilder.WireIfLoaded(WireWavefrontMetrics, SteeltoeAssemblies.SteeltoeManagementEndpointCore);

        if (!webApplicationBuilder.WireIfLoaded(WireDistributedTracingCore, SteeltoeAssemblies.SteeltoeManagementTracingCore))
        {
            webApplicationBuilder.WireIfLoaded(WireDistributedTracingBase, SteeltoeAssemblies.SteeltoeManagementTracingBase);
        }

        webApplicationBuilder.WireIfLoaded(WireCloudFoundryContainerIdentity, SteeltoeAssemblies.SteeltoeSecurityAuthenticationCloudFoundryCore);
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

    private static bool WireIfAnyLoaded(this WebApplicationBuilder webApplicationBuilder, Action<WebApplicationBuilder> action, params string[] assembly)
    {
        if (assembly.Any(AssemblyExtensions.IsAssemblyLoaded))
        {
            action(webApplicationBuilder);
            return true;
        }

        return false;
    }

    private static void Log(this IServiceCollection obj, string message)
    {
        _logger.LogInformation(message);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireConfigServer(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Configuration.AddConfigServer(webApplicationBuilder.Environment.EnvironmentName, _loggerFactory);
        webApplicationBuilder.Services.AddConfigServerServices();
        webApplicationBuilder.Services.Log(LogMessages.WireConfigServer);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireCloudFoundryConfiguration(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Configuration.AddCloudFoundry();
        webApplicationBuilder.Services.Log(LogMessages.WireCloudFoundryConfiguration);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireKubernetesConfiguration(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Configuration.AddKubernetes(loggerFactory: _loggerFactory);
        webApplicationBuilder.Services.AddKubernetesConfigurationServices();
        webApplicationBuilder.Services.Log(LogMessages.WireKubernetesConfiguration);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireRandomValueProvider(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Configuration.AddRandomValueSource(_loggerFactory);
        webApplicationBuilder.Services.Log(LogMessages.WireRandomValueProvider);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WirePlaceholderResolver(this WebApplicationBuilder webApplicationBuilder)
    {
        ((IConfigurationBuilder)webApplicationBuilder.Configuration).AddPlaceholderResolver(_loggerFactory);
        webApplicationBuilder.Services.Log(LogMessages.WirePlaceholderResolver);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireConnectorConfiguration(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Configuration.AddConnectionStrings();
        webApplicationBuilder.Services.Log(LogMessages.WireConnectorsConfiguration);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireMySqlConnection(this WebApplicationBuilder webApplicationBuilder) =>
        webApplicationBuilder.Services.AddMySqlConnection(webApplicationBuilder.Configuration).Log(LogMessages.WireMySqlConnection);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireMongoClient(this WebApplicationBuilder webApplicationBuilder) =>
        webApplicationBuilder.Services.AddMongoClient(webApplicationBuilder.Configuration).Log(LogMessages.WireMongoClient);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireOracleConnection(this WebApplicationBuilder webApplicationBuilder) =>
        webApplicationBuilder.Services.AddOracleConnection(webApplicationBuilder.Configuration).Log(LogMessages.WireOracleConnection);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WirePostgresConnection(this WebApplicationBuilder webApplicationBuilder) =>
        webApplicationBuilder.Services.AddPostgresConnection(webApplicationBuilder.Configuration).Log(LogMessages.WirePostgresConnection);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireRabbitMqConnection(this WebApplicationBuilder webApplicationBuilder) =>
        webApplicationBuilder.Services.AddRabbitMQConnection(webApplicationBuilder.Configuration).Log(LogMessages.WireRabbitMqConnection);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireRedisConnectionMultiplexer(this WebApplicationBuilder webApplicationBuilder) =>
        webApplicationBuilder.Services.AddRedisConnectionMultiplexer(webApplicationBuilder.Configuration).Log(LogMessages.WireRedisConnectionMultiplexer);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireDistributedRedisCache(this WebApplicationBuilder webApplicationBuilder) =>
        webApplicationBuilder.Services.AddDistributedRedisCache(webApplicationBuilder.Configuration).Log(LogMessages.WireDistributedRedisCache);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireSqlServerConnection(this WebApplicationBuilder webApplicationBuilder) =>
        webApplicationBuilder.Services.AddSqlServerConnection(webApplicationBuilder.Configuration).Log(LogMessages.WireSqlServerConnection);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireDiscoveryClient(this WebApplicationBuilder webApplicationBuilder) =>
        webApplicationBuilder.Services.AddDiscoveryClient(webApplicationBuilder.Configuration).Log(LogMessages.WireDiscoveryClient);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireDistributedTracingBase(this WebApplicationBuilder webApplicationBuilder) =>
        webApplicationBuilder.Services.AddDistributedTracing().Log(LogMessages.WireDistributedTracing);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireDistributedTracingCore(this WebApplicationBuilder webApplicationBuilder) =>
        webApplicationBuilder.Services.AddDistributedTracingAspNetCore().Log(LogMessages.WireDistributedTracing);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireKubernetesActuators(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Services.AddKubernetesActuators(webApplicationBuilder.Configuration);
        webApplicationBuilder.Services.ActivateActuatorEndpoints();
        webApplicationBuilder.Services.Log(LogMessages.WireKubernetesActuators);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireAllActuators(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Services.AddAllActuators(webApplicationBuilder.Configuration);
        webApplicationBuilder.Services.ActivateActuatorEndpoints();
        webApplicationBuilder.Services.Log(LogMessages.WireAllActuators);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireWavefrontMetrics(this WebApplicationBuilder webApplicationBuilder)
    {
        if (!webApplicationBuilder.Configuration.HasWavefront())
        {
            return;
        }

        webApplicationBuilder.AddWavefrontMetrics();
        webApplicationBuilder.Services.Log(LogMessages.WireWavefrontMetrics);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireDynamicSerilog(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Logging.AddDynamicSerilog();
        webApplicationBuilder.Services.Log(LogMessages.WireDynamicSerilog);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireCloudFoundryContainerIdentity(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Configuration.AddCloudFoundryContainerIdentity();
        webApplicationBuilder.Services.AddCloudFoundryCertificateAuth();
        webApplicationBuilder.Services.Log(LogMessages.WireCloudFoundryContainerIdentity);
    }
}
