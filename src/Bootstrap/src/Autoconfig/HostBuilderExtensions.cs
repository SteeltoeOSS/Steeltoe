// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#pragma warning disable 0436
#pragma warning disable S1144

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
using Steeltoe.Extensions.Logging;
using Steeltoe.Extensions.Logging.DynamicSerilog;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Kubernetes;
using Steeltoe.Management.TaskCore;
using Steeltoe.Management.Tracing;
using Steeltoe.Security.Authentication.CloudFoundry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Steeltoe.Bootstrap.Autoconfig
{
    public static class HostBuilderExtensions
    {
        private const string LoggerName = "Steeltoe.Autoconfig";
        private static HashSet<string> missingAssemblies = new ();

        static HostBuilderExtensions()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += LoadAnyVersion;
            Assembly LoadAnyVersion(object sender, ResolveEventArgs args)
            {
                Assembly assembly = null;

                // Load whatever version available - strip out version and culture info
                string GetSimpleName(string assemblyName) => new Regex(",.*").Replace(assemblyName, string.Empty);
                var name = GetSimpleName(args.Name);
                if (missingAssemblies.Contains(name))
                {
                    return null;
                }

                var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToDictionary(x => x.GetName().Name, x => x);
                if (assemblies.TryGetValue(name, out assembly))
                {
                    return assembly;
                }

                if (args.Name?.Contains(".resources") ?? false)
                {
                    return args.RequestingAssembly;
                }

                missingAssemblies.Add(name); // throw it in there to prevent recursive attempts to resolve
                assembly = Assembly.Load(name);
                missingAssemblies.Remove(name);
                return assembly;
            }
        }

        public static IHostBuilder AddSteeltoe(this IHostBuilder hostBuilder, ILoggerFactory loggerFactory = null)
        {
            var logger = loggerFactory?.CreateLogger(LoggerName) ?? NullLogger.Instance;
            hostBuilder.Properties[LoggerName] = logger;

            if (!hostBuilder.WireIfLoaded(WireConfigServer, SteeltoeAssemblies.Steeltoe_Extensions_Configuration_ConfigServerCore))
            {
                hostBuilder.WireIfLoaded(WireCloudFoundryConfiguration, SteeltoeAssemblies.Steeltoe_Extensions_Configuration_CloudFoundryCore);
            }

            if (IsAssemblyLoaded(SteeltoeAssemblies.Steeltoe_Extensions_Configuration_KubernetesCore) && IsRunningInKubernetes())
            {
                WireKubernetesConfiguration(hostBuilder);
            }

            hostBuilder.WireIfLoaded(WirePlaceholderResolver, SteeltoeAssemblies.Steeltoe_Extensions_Configuration_PlaceholderCore);

            if (IsAssemblyLoaded(SteeltoeAssemblies.Steeltoe_Connector_ConnectorCore))
            {
                hostBuilder.WireIfAnyLoaded(WireMySqlConnection, MySqlTypeLocator.Assemblies);
                hostBuilder.WireIfAnyLoaded(WireMongoClient, MongoDbTypeLocator.Assemblies);
                hostBuilder.WireIfAnyLoaded(WireOracleConnection, OracleTypeLocator.Assemblies);
                hostBuilder.WireIfAnyLoaded(WirePostgresConnection, PostgreSqlTypeLocator.Assemblies);
                hostBuilder.WireIfAnyLoaded(WireRabbitMqConnection, RabbitMQTypeLocator.Assemblies);
                hostBuilder.WireIfAnyLoaded(WireRedisConnectionMultiplexer, RedisTypeLocator.StackExchangeAssemblies);
                hostBuilder.WireIfAnyLoaded(WireDistributedRedisCache, RedisTypeLocator.MicrosoftAssemblies.Except(new[] { "Microsoft.Extensions.Caching.Abstractions" }).ToArray());
                hostBuilder.WireIfAnyLoaded(WireSqlServerConnection, SqlServerTypeLocator.Assemblies);
            }

            hostBuilder.WireIfLoaded(WireKubernetesActuators, SteeltoeAssemblies.Steeltoe_Management_KubernetesCore);
            hostBuilder.WireIfLoaded(WireDiscoveryClient, SteeltoeAssemblies.Steeltoe_Discovery_ClientCore);
            hostBuilder.WireIfLoaded(WireCloudFoundryActuators, SteeltoeAssemblies.Steeltoe_Management_CloudFoundryCore);
            hostBuilder.WireIfLoaded(WireDistributedTracing, SteeltoeAssemblies.Steeltoe_Management_TracingCore);

            if (!hostBuilder.WireIfLoaded(WireDynamicSerilog, SteeltoeAssemblies.Steeltoe_Extensions_Logging_DynamicSerilogCore))
            {
                hostBuilder.WireIfLoaded(WireDynamicLogging, SteeltoeAssemblies.Steeltoe_Extensions_Logging_DynamicLogger);
            }

            hostBuilder.WireIfLoaded(WireCloudFoundryContainerIdentity, SteeltoeAssemblies.Steeltoe_Security_Authentication_CloudFoundryCore);
            return hostBuilder;
        }

        private static bool IsAssemblyLoaded(string typeName)
        {
            try
            {
                Assembly.Load(typeName);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool WireIfLoaded(this IHostBuilder hostBuilder, Action<IHostBuilder> action, params string[] assembly)
        {
            if (assembly.All(IsAssemblyLoaded))
            {
                action(hostBuilder);
                return true;
            }

            return false;
        }

        private static bool WireIfAnyLoaded(this IHostBuilder hostBuilder, Action<IHostBuilder> action, params string[] assembly)
        {
            if (assembly.Any(IsAssemblyLoaded))
            {
                action(hostBuilder);
                return true;
            }

            return false;
        }

        // ReSharper disable once S1144
        // ReSharper disable once UnusedMember.Local
        private static bool IsRunningInCloudFoundry() => Environment.GetEnvironmentVariable("VCAP_APPLICATION") != null;

        private static bool IsRunningInKubernetes() => Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST") != null;

        private static IHostBuilder Log(this IHostBuilder host, string message)
        {
            var logger = (ILogger)host.Properties[LoggerName];
            logger.LogInformation(message);
            return host;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WireKubernetesActuators(this IHostBuilder hostBuilder) =>
            hostBuilder.AddKubernetesActuators().Log(LogMessages.WireKubernetesActuators);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WireConfigServer(this IHostBuilder hostBuilder) =>
            hostBuilder.AddConfigServer().Log(LogMessages.WireConfigServer);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WireCloudFoundryConfiguration(this IHostBuilder hostBuilder) =>
            hostBuilder.AddCloudFoundryConfiguration().Log(LogMessages.WireCloudFoundryConfiguration);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WireKubernetesConfiguration(this IHostBuilder hostBuilder) =>
            hostBuilder.AddKubernetesConfiguration().Log(LogMessages.WireKubernetesConfiguration);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WirePlaceholderResolver(this IHostBuilder hostBuilder) =>
            hostBuilder.AddPlaceholderResolver().Log(LogMessages.WirePlaceholderResolver);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WireMySqlConnection(this IHostBuilder hostBuilder) =>
            hostBuilder.ConfigureServices((host, svc) => svc.AddMySqlConnection(host.Configuration)).Log(LogMessages.WireMySqlConnection);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WireMongoClient(this IHostBuilder hostBuilder) =>
            hostBuilder.ConfigureServices((host, svc) => svc.AddMongoClient(host.Configuration)).Log(LogMessages.WireMongoClient);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WireOracleConnection(this IHostBuilder hostBuilder) =>
            hostBuilder.ConfigureServices((host, svc) => svc.AddOracleConnection(host.Configuration)).Log(LogMessages.WireOracleConnection);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WirePostgresConnection(this IHostBuilder hostBuilder) =>
            hostBuilder.ConfigureServices((host, svc) => svc.AddPostgresConnection(host.Configuration)).Log(LogMessages.WirePostgresConnection);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WireRabbitMqConnection(this IHostBuilder hostBuilder) =>
            hostBuilder.ConfigureServices((host, svc) => svc.AddRabbitMQConnection(host.Configuration)).Log(LogMessages.WireRabbitMqConnection);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WireRedisConnectionMultiplexer(this IHostBuilder hostBuilder) =>
            hostBuilder.ConfigureServices((host, svc) => svc.AddRedisConnectionMultiplexer(host.Configuration)).Log(LogMessages.WireRedisConnectionMultiplexer);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WireDistributedRedisCache(this IHostBuilder hostBuilder) =>
            hostBuilder.ConfigureServices((host, svc) => svc.AddDistributedRedisCache(host.Configuration)).Log(LogMessages.WireDistributedRedisCache);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WireSqlServerConnection(this IHostBuilder hostBuilder) =>
            hostBuilder.ConfigureServices((host, svc) => svc.AddSqlServerConnection(host.Configuration)).Log(LogMessages.WireSqlServerConnection);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WireDistributedTracing(this IHostBuilder hostBuilder) =>
            hostBuilder.ConfigureServices((host, svc) => svc.AddDistributedTracing(host.Configuration)).Log(LogMessages.WireDistributedTracing);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WireDiscoveryClient(this IHostBuilder hostBuilder) => hostBuilder.AddDiscoveryClient().Log(LogMessages.WireDiscoveryClient);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WireCloudFoundryActuators(this IHostBuilder hostBuilder) => hostBuilder.AddCloudFoundryActuator().Log(LogMessages.WireCloudFoundryActuators);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WireDynamicSerilog(this IHostBuilder hostBuilder) => hostBuilder.AddDynamicSerilog().Log(LogMessages.WireDynamicSerilog);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WireDynamicLogging(this IHostBuilder hostBuilder) => hostBuilder.AddDynamicLogging().Log(LogMessages.WireDynamicLogging);

        private static void WireCloudFoundryContainerIdentity(this IHostBuilder hostBuilder) => hostBuilder
            .ConfigureAppConfiguration(cfg => cfg.AddCloudFoundryContainerIdentity())
            .ConfigureServices((host, svc) => svc.AddCloudFoundryCertificateAuth(host.Configuration))
            .Log(LogMessages.WireCloudFoundryContainerIdentity);
    }
}