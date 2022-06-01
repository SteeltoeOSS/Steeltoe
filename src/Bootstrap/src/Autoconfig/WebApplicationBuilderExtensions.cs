// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Steeltoe.Bootstrap.Autoconfig;

public static class WebApplicationBuilderExtensions
{
    private const string _loggerName = "Steeltoe.Autoconfig";
    private static ILoggerFactory _loggerFactory;
    private static ILogger _logger;

    static WebApplicationBuilderExtensions()
    {
        var currentDomain = AppDomain.CurrentDomain;
        currentDomain.AssemblyResolve += AssemblyExtensions.LoadAnyVersion;
    }

    /// <summary>
    /// Automatically configure Steeltoe packages that have been added as NuGet references.<para />
    /// PLEASE NOTE: No extensions to IApplicationBuilder will be configured!
    /// </summary>
    /// <param name="webApplicationBuilder">Your <see cref="WebApplicationBuilder" /></param>
    /// <param name="exclusions">A list of assemblies to exclude from auto-configuration. For ease of use, select from <see cref="SteeltoeAssemblies" /></param>
    /// <param name="loggerFactory">For logging within auto-configuration</param>
    public static WebApplicationBuilder AddSteeltoe(this WebApplicationBuilder webApplicationBuilder, IEnumerable<string> exclusions = null, ILoggerFactory loggerFactory = null)
    {
        AssemblyExtensions.ExcludedAssemblies = exclusions ?? new List<string>();
        _loggerFactory = loggerFactory;
        _logger = loggerFactory?.CreateLogger(_loggerName) ?? NullLogger.Instance;

        if (!webApplicationBuilder.WireIfAnyLoaded(WireConfigServer, SteeltoeAssemblies.Steeltoe_Extensions_Configuration_ConfigServerBase, SteeltoeAssemblies.Steeltoe_Extensions_Configuration_ConfigServerCore))
        {
            webApplicationBuilder.WireIfAnyLoaded(WireCloudFoundryConfiguration, SteeltoeAssemblies.Steeltoe_Extensions_Configuration_CloudFoundryBase, SteeltoeAssemblies.Steeltoe_Extensions_Configuration_CloudFoundryCore);
        }

        if (Platform.IsKubernetes && AssemblyExtensions.IsEitherAssemblyLoaded(SteeltoeAssemblies.Steeltoe_Extensions_Configuration_KubernetesBase, SteeltoeAssemblies.Steeltoe_Extensions_Configuration_KubernetesCore))
        {
            WireKubernetesConfiguration(webApplicationBuilder);
        }

        webApplicationBuilder.WireIfLoaded(WireRandomValueProvider, SteeltoeAssemblies.Steeltoe_Extensions_Configuration_RandomValueBase);
        webApplicationBuilder.WireIfAnyLoaded(WirePlaceholderResolver, SteeltoeAssemblies.Steeltoe_Extensions_Configuration_PlaceholderBase, SteeltoeAssemblies.Steeltoe_Extensions_Configuration_PlaceholderCore);

        if (webApplicationBuilder.WireIfLoaded(WireConnectorConfiguration, SteeltoeAssemblies.Steeltoe_Connector_ConnectorCore))
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

        webApplicationBuilder.WireIfLoaded(WireDynamicSerilog, SteeltoeAssemblies.Steeltoe_Extensions_Logging_DynamicSerilogCore);
        webApplicationBuilder.WireIfAnyLoaded(WireDiscoveryClient, SteeltoeAssemblies.Steeltoe_Discovery_ClientBase, SteeltoeAssemblies.Steeltoe_Discovery_ClientCore);

        if (AssemblyExtensions.IsAssemblyLoaded(SteeltoeAssemblies.Steeltoe_Management_KubernetesCore))
        {
            webApplicationBuilder.WireIfLoaded(WireKubernetesActuators, SteeltoeAssemblies.Steeltoe_Management_KubernetesCore);
        }
        else
        {
            webApplicationBuilder.WireIfLoaded(WireAllActuators, SteeltoeAssemblies.Steeltoe_Management_EndpointCore);
        }

        webApplicationBuilder.WireIfLoaded(WireWavefrontMetrics, SteeltoeAssemblies.Steeltoe_Management_EndpointCore);

        if (!webApplicationBuilder.WireIfLoaded(WireDistributedTracingCore, SteeltoeAssemblies.Steeltoe_Management_TracingCore))
        {
            webApplicationBuilder.WireIfLoaded(WireDistributedTracingBase, SteeltoeAssemblies.Steeltoe_Management_TracingBase);
        }

        webApplicationBuilder.WireIfLoaded(WireCloudFoundryContainerIdentity, SteeltoeAssemblies.Steeltoe_Security_Authentication_CloudFoundryCore);
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

    private static object Log(this object obj, string message)
    {
        _logger.LogInformation(message);
        return obj;
    }

    #region Config Providers
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireConfigServer(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Configuration.AddConfigServer(webApplicationBuilder.Environment.EnvironmentName, _loggerFactory);
        webApplicationBuilder.Services.AddConfigServerServices();
        webApplicationBuilder.Log(LogMessages.WireConfigServer);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireCloudFoundryConfiguration(this WebApplicationBuilder webApplicationBuilder) =>
        webApplicationBuilder.Configuration.AddCloudFoundry().Log(LogMessages.WireCloudFoundryConfiguration);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireKubernetesConfiguration(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Configuration.AddKubernetes(loggerFactory: _loggerFactory);
        webApplicationBuilder.Services.AddKubernetesConfigurationServices();
        webApplicationBuilder.Log(LogMessages.WireKubernetesConfiguration);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireRandomValueProvider(this WebApplicationBuilder webApplicationBuilder) =>
         webApplicationBuilder.Configuration.AddRandomValueSource(_loggerFactory).Log(LogMessages.WireRandomValueProvider);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WirePlaceholderResolver(this WebApplicationBuilder webApplicationBuilder) =>
        ((IConfigurationBuilder)webApplicationBuilder.Configuration).AddPlaceholderResolver(_loggerFactory).Log(LogMessages.WirePlaceholderResolver);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireConnectorConfiguration(this WebApplicationBuilder webApplicationBuilder) =>
        webApplicationBuilder.Configuration.AddConnectionStrings().Log(LogMessages.WireConnectorsConfiguration);
    #endregion

    #region Connectors
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
    #endregion

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
        webApplicationBuilder.Log(LogMessages.WireKubernetesActuators);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireAllActuators(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Services.AddAllActuators(webApplicationBuilder.Configuration);
        webApplicationBuilder.Services.ActivateActuatorEndpoints();
        webApplicationBuilder.Log(LogMessages.WireAllActuators);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireWavefrontMetrics(this WebApplicationBuilder webApplicationBuilder)
    {
        if (!webApplicationBuilder.Configuration.HasWavefront())
        {
            return;
        }

        webApplicationBuilder.AddWavefrontMetrics().Log(LogMessages.WireWavefrontMetrics);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireDynamicSerilog(this WebApplicationBuilder webApplicationBuilder) =>
        webApplicationBuilder.Logging.AddDynamicSerilog().Log(LogMessages.WireDynamicSerilog);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireCloudFoundryContainerIdentity(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Configuration.AddCloudFoundryContainerIdentity();
        webApplicationBuilder.Services.AddCloudFoundryCertificateAuth();
        webApplicationBuilder.Log(LogMessages.WireCloudFoundryContainerIdentity);
    }
}
#endif
