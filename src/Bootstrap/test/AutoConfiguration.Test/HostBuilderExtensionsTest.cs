// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using MySqlConnector;
using Npgsql;
using OpenTelemetry.Metrics;
using RabbitMQ.Client;
using StackExchange.Redis;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Configuration.CloudFoundry.ServiceBindings;
using Steeltoe.Configuration.ConfigServer;
using Steeltoe.Configuration.Encryption;
using Steeltoe.Configuration.Kubernetes.ServiceBindings;
using Steeltoe.Configuration.Placeholder;
using Steeltoe.Configuration.RandomValue;
using Steeltoe.Configuration.SpringBoot;
using Steeltoe.Connectors;
using Steeltoe.Connectors.CosmosDb;
using Steeltoe.Connectors.MongoDb;
using Steeltoe.Connectors.MySql;
using Steeltoe.Connectors.PostgreSql;
using Steeltoe.Connectors.RabbitMQ;
using Steeltoe.Connectors.Redis;
using Steeltoe.Connectors.SqlServer;
using Steeltoe.Discovery.Configuration;
using Steeltoe.Discovery.Consul;
using Steeltoe.Discovery.Eureka;
using Steeltoe.Logging;
using Steeltoe.Logging.DynamicConsole;
using Steeltoe.Logging.DynamicSerilog;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Actuators.Health.Availability;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Tracing;

namespace Steeltoe.Bootstrap.AutoConfiguration.Test;

public sealed class HostBuilderExtensionsTest
{
    private static readonly Dictionary<string, string?> HealthProbeSettings = new()
    {
        ["Management:Endpoints:Health:Liveness:Enabled"] = "true",
        ["Management:Endpoints:Health:Readiness:Enabled"] = "true",
        ["Management:Endpoints:Health:ShowComponents"] = "Always"
    };

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    [InlineData(HostBuilderType.HostApplication)]
    public async Task ConfigServerConfiguration_IsAutowired(HostBuilderType hostBuilderType)
    {
        await using HostWrapper hostWrapper = HostWrapperFactory.GetForOnly(SteeltoeAssemblyNames.ConfigurationConfigServer, hostBuilderType);
        await hostWrapper.StartAsync(TestContext.Current.CancellationToken);

        AssertConfigServerConfigurationIsAutowired(hostWrapper);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    [InlineData(HostBuilderType.HostApplication)]
    public async Task CloudFoundryConfiguration_IsAutowired(HostBuilderType hostBuilderType)
    {
        await using HostWrapper hostWrapper = HostWrapperFactory.GetForOnly(SteeltoeAssemblyNames.ConfigurationCloudFoundry, hostBuilderType);
        await hostWrapper.StartAsync(TestContext.Current.CancellationToken);

        AssertCloudFoundryConfigurationIsAutowired(hostWrapper);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    [InlineData(HostBuilderType.HostApplication)]
    public async Task RandomValueConfiguration_IsAutowired(HostBuilderType hostBuilderType)
    {
        await using HostWrapper hostWrapper = HostWrapperFactory.GetForOnly(SteeltoeAssemblyNames.ConfigurationRandomValue, hostBuilderType);
        await hostWrapper.StartAsync(TestContext.Current.CancellationToken);

        AssertRandomValueConfigurationIsAutowired(hostWrapper);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    [InlineData(HostBuilderType.HostApplication)]
    public async Task SpringBootConfiguration_IsAutowired(HostBuilderType hostBuilderType)
    {
        await using HostWrapper hostWrapper = HostWrapperFactory.GetForOnly(SteeltoeAssemblyNames.ConfigurationSpringBoot, hostBuilderType);
        await hostWrapper.StartAsync(TestContext.Current.CancellationToken);

        AssertSpringBootConfigurationIsAutowired(hostWrapper);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    [InlineData(HostBuilderType.HostApplication)]
    public async Task EncryptionConfiguration_IsAutowired(HostBuilderType hostBuilderType)
    {
        await using HostWrapper hostWrapper = HostWrapperFactory.GetForOnly(SteeltoeAssemblyNames.ConfigurationEncryption, hostBuilderType);
        await hostWrapper.StartAsync(TestContext.Current.CancellationToken);

        AssertEncryptionConfigurationIsAutowired(hostWrapper);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    [InlineData(HostBuilderType.HostApplication)]
    public async Task PlaceholderResolver_IsAutowired(HostBuilderType hostBuilderType)
    {
        await using HostWrapper hostWrapper = HostWrapperFactory.GetForOnly(SteeltoeAssemblyNames.ConfigurationPlaceholder, hostBuilderType);
        await hostWrapper.StartAsync(TestContext.Current.CancellationToken);

        AssertPlaceholderResolverIsAutowired(hostWrapper);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    [InlineData(HostBuilderType.HostApplication)]
    public async Task Connectors_AreAutowired(HostBuilderType hostBuilderType)
    {
        await using HostWrapper hostWrapper = HostWrapperFactory.GetForOnly(SteeltoeAssemblyNames.Connectors, hostBuilderType);
        await hostWrapper.StartAsync(TestContext.Current.CancellationToken);

        AssertConnectorsAreAutowired(hostWrapper);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    [InlineData(HostBuilderType.HostApplication)]
    public async Task SqlServerConnector_NotAutowiredIfDependenciesExcluded(HostBuilderType hostBuilderType)
    {
        var exclusions = new HashSet<string>(SteeltoeAssemblyNames.All);
        exclusions.Remove(SteeltoeAssemblyNames.Connectors);
        exclusions.Add("Microsoft.Data.SqlClient");
        exclusions.Add("System.Data.SqlClient");

        await using HostWrapper hostWrapper = HostWrapperFactory.GetExcluding(exclusions, hostBuilderType);
        await hostWrapper.StartAsync(TestContext.Current.CancellationToken);

        hostWrapper.Services.GetService<ConnectorFactory<SqlServerOptions, SqlConnection>>().Should().BeNull();
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    [InlineData(HostBuilderType.HostApplication)]
    public async Task DynamicSerilog_IsAutowired(HostBuilderType hostBuilderType)
    {
        await using HostWrapper hostWrapper = HostWrapperFactory.GetForOnly(SteeltoeAssemblyNames.LoggingDynamicSerilog, hostBuilderType);
        await hostWrapper.StartAsync(TestContext.Current.CancellationToken);

        AssertDynamicSerilogIsAutowired(hostWrapper);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    [InlineData(HostBuilderType.HostApplication)]
    public async Task DynamicConsoleLogger_IsAutowired(HostBuilderType hostBuilderType)
    {
        await using HostWrapper hostWrapper = HostWrapperFactory.GetForOnly(SteeltoeAssemblyNames.LoggingDynamicConsole, hostBuilderType);
        await hostWrapper.StartAsync(TestContext.Current.CancellationToken);

        AssertDynamicConsoleIsAutowired(hostWrapper);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    [InlineData(HostBuilderType.HostApplication)]
    public async Task ServiceDiscoveryClients_AreAutowired(HostBuilderType hostBuilderType)
    {
        var assembliesToInclude = new HashSet<string>
        {
            SteeltoeAssemblyNames.DiscoveryConfiguration,
            SteeltoeAssemblyNames.DiscoveryConsul,
            SteeltoeAssemblyNames.DiscoveryEureka
        };

        await using HostWrapper hostWrapper =
            HostWrapperFactory.GetExcluding(SteeltoeAssemblyNames.All.Except(assembliesToInclude).ToHashSet(), hostBuilderType);

        await hostWrapper.StartAsync(TestContext.Current.CancellationToken);

        AssertServiceDiscoveryClientsAreAutowired(hostWrapper);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    [InlineData(HostBuilderType.HostApplication)]
    public async Task Prometheus_IsAutowired(HostBuilderType hostBuilderType)
    {
        await using HostWrapper hostWrapper = HostWrapperFactory.GetForOnly(SteeltoeAssemblyNames.ManagementPrometheus, hostBuilderType);
        await hostWrapper.StartAsync(TestContext.Current.CancellationToken);

        AssertPrometheusIsAutowired(hostWrapper);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task AllActuators_AreAutowired(HostBuilderType hostBuilderType)
    {
        await using HostWrapper hostWrapper = HostWrapperFactory.GetForOnly(SteeltoeAssemblyNames.ManagementEndpoint, hostBuilderType);
        await hostWrapper.StartAsync(TestContext.Current.CancellationToken);

        await AssertAllActuatorsAreAutowiredAsync(hostWrapper, true, TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    [InlineData(HostBuilderType.HostApplication)]
    public async Task Tracing_IsAutowired(HostBuilderType hostBuilderType)
    {
        await using HostWrapper hostWrapper = HostWrapperFactory.GetForOnly(SteeltoeAssemblyNames.ManagementTracing, hostBuilderType);
        await hostWrapper.StartAsync(TestContext.Current.CancellationToken);

        AssertTracingIsAutowired(hostWrapper);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task Everything_IsAutowired(HostBuilderType hostBuilderType)
    {
        await using HostWrapper hostWrapper = HostWrapperFactory.GetExcluding(new HashSet<string>(), hostBuilderType);
        await hostWrapper.StartAsync(TestContext.Current.CancellationToken);

        AssertConfigServerConfigurationIsAutowired(hostWrapper);
        AssertCloudFoundryConfigurationIsAutowired(hostWrapper);
        AssertRandomValueConfigurationIsAutowired(hostWrapper);
        AssertSpringBootConfigurationIsAutowired(hostWrapper);
        AssertEncryptionConfigurationIsAutowired(hostWrapper);
        AssertPlaceholderResolverIsAutowired(hostWrapper);
        AssertConnectorsAreAutowired(hostWrapper);
        AssertDynamicSerilogIsAutowired(hostWrapper);
        AssertServiceDiscoveryClientsAreAutowired(hostWrapper);
        AssertPrometheusIsAutowired(hostWrapper);
        AssertTracingIsAutowired(hostWrapper);

        await AssertAllActuatorsAreAutowiredAsync(hostWrapper, false, TestContext.Current.CancellationToken);
    }

    private static void AssertConfigServerConfigurationIsAutowired(HostWrapper hostWrapper)
    {
        var configuration = hostWrapper.Services.GetRequiredService<IConfiguration>();

        configuration.EnumerateProviders<CloudFoundryConfigurationProvider>().Should().ContainSingle();
        configuration.EnumerateProviders<ConfigServerConfigurationProvider>().Should().ContainSingle();
    }

    private static void AssertCloudFoundryConfigurationIsAutowired(HostWrapper hostWrapper)
    {
        var configuration = hostWrapper.Services.GetRequiredService<IConfiguration>();

        configuration.EnumerateProviders<CloudFoundryConfigurationProvider>().Should().ContainSingle();
    }

    private static void AssertRandomValueConfigurationIsAutowired(HostWrapper hostWrapper)
    {
        var configuration = hostWrapper.Services.GetRequiredService<IConfiguration>();

        configuration.EnumerateProviders<RandomValueProvider>().Should().ContainSingle();
    }

    private static void AssertSpringBootConfigurationIsAutowired(HostWrapper hostWrapper)
    {
        var configuration = hostWrapper.Services.GetRequiredService<IConfiguration>();

        configuration.EnumerateProviders<SpringBootEnvironmentVariableProvider>().Should().ContainSingle();
        configuration.EnumerateProviders<SpringBootCommandLineProvider>().Should().ContainSingle();
    }

    private static void AssertEncryptionConfigurationIsAutowired(HostWrapper hostWrapper)
    {
        var configurationRoot = (IConfigurationRoot)hostWrapper.Services.GetRequiredService<IConfiguration>();

        configurationRoot.EnumerateProviders<DecryptionConfigurationProvider>().Should().ContainSingle();
    }

    private static void AssertPlaceholderResolverIsAutowired(HostWrapper hostWrapper)
    {
        var configurationRoot = (IConfigurationRoot)hostWrapper.Services.GetRequiredService<IConfiguration>();

        configurationRoot.EnumerateProviders<PlaceholderConfigurationProvider>().Should().ContainSingle();
    }

    private static void AssertConnectorsAreAutowired(HostWrapper hostWrapper)
    {
        var configuration = hostWrapper.Services.GetRequiredService<IConfiguration>();

        configuration.EnumerateProviders<KubernetesServiceBindingConfigurationProvider>().Should().NotBeEmpty();
        configuration.EnumerateProviders<CloudFoundryServiceBindingConfigurationProvider>().Should().NotBeEmpty();

        hostWrapper.Services.GetService<ConnectorFactory<CosmosDbOptions, CosmosClient>>().Should().NotBeNull();
        hostWrapper.Services.GetService<ConnectorFactory<MongoDbOptions, IMongoClient>>().Should().NotBeNull();
        hostWrapper.Services.GetService<ConnectorFactory<MySqlOptions, MySqlConnection>>().Should().NotBeNull();
        hostWrapper.Services.GetService<ConnectorFactory<PostgreSqlOptions, NpgsqlConnection>>().Should().NotBeNull();
        hostWrapper.Services.GetService<ConnectorFactory<RabbitMQOptions, IConnection>>().Should().NotBeNull();
        hostWrapper.Services.GetService<ConnectorFactory<RedisOptions, IConnectionMultiplexer>>().Should().NotBeNull();
        hostWrapper.Services.GetService<ConnectorFactory<RedisOptions, IDistributedCache>>().Should().NotBeNull();
        hostWrapper.Services.GetService<ConnectorFactory<SqlServerOptions, SqlConnection>>().Should().NotBeNull();
    }

    private static void AssertDynamicSerilogIsAutowired(HostWrapper hostWrapper)
    {
        var loggerProvider = hostWrapper.Services.GetRequiredService<IDynamicLoggerProvider>();

        loggerProvider.Should().BeOfType<DynamicSerilogLoggerProvider>();
    }

    private static void AssertDynamicConsoleIsAutowired(HostWrapper hostWrapper)
    {
        var loggerProvider = hostWrapper.Services.GetRequiredService<IDynamicLoggerProvider>();

        loggerProvider.Should().BeOfType<DynamicConsoleLoggerProvider>();
    }

    private static void AssertServiceDiscoveryClientsAreAutowired(HostWrapper hostWrapper)
    {
        IDiscoveryClient[] discoveryClients = [.. hostWrapper.Services.GetServices<IDiscoveryClient>()];

        discoveryClients.Should().HaveCount(3);
        discoveryClients.OfType<ConfigurationDiscoveryClient>().Should().ContainSingle();
        discoveryClients.OfType<ConsulDiscoveryClient>().Should().ContainSingle();
        discoveryClients.OfType<EurekaDiscoveryClient>().Should().ContainSingle();
    }

    private static void AssertPrometheusIsAutowired(HostWrapper hostWrapper)
    {
        hostWrapper.Services.GetService<MeterProvider>().Should().NotBeNull();
    }

    private static async Task AssertAllActuatorsAreAutowiredAsync(HostWrapper hostWrapper, bool expectHealthy, CancellationToken cancellationToken)
    {
        hostWrapper.Services.GetServices<IHypermediaEndpointHandler>().Should().ContainSingle();
        hostWrapper.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();

        var availability = hostWrapper.Services.GetRequiredService<ApplicationAvailability>();
        availability.GetLivenessState().Should().Be(LivenessState.Correct);
        availability.GetReadinessState().Should().Be(ReadinessState.AcceptingTraffic);

        using HttpClient httpClient = hostWrapper.GetTestClient();

        using HttpResponseMessage response1 = await httpClient.GetAsync(new Uri("/actuator", UriKind.Relative), cancellationToken);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        using HttpResponseMessage response2 = await httpClient.GetAsync(new Uri("/actuator/info", UriKind.Relative), cancellationToken);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        using HttpResponseMessage response3 = await httpClient.GetAsync(new Uri("/actuator/health", UriKind.Relative), cancellationToken);
        response3.StatusCode.Should().Be(expectHealthy ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable);

        using HttpResponseMessage response4 = await httpClient.GetAsync(new Uri("/actuator/health/liveness", UriKind.Relative), cancellationToken);
        response4.StatusCode.Should().Be(HttpStatusCode.OK);
        string responseContent = await response4.Content.ReadAsStringAsync(cancellationToken);

        responseContent.Should().BeJson("""
            {
              "status": "UP",
              "components": {
                "livenessState": {
                  "status": "UP"
                }
              }
            }
            """);

        using HttpResponseMessage response5 = await httpClient.GetAsync(new Uri("/actuator/health/readiness", UriKind.Relative), cancellationToken);
        response5.StatusCode.Should().Be(HttpStatusCode.OK);
        responseContent = await response5.Content.ReadAsStringAsync(cancellationToken);

        responseContent.Should().BeJson("""
            {
              "status": "UP",
              "components": {
                "readinessState": {
                  "status": "UP"
                }
              }
            }
            """);
    }

    private static void AssertTracingIsAutowired(HostWrapper hostWrapper)
    {
        var applicationInstanceInfo = hostWrapper.Services.GetRequiredService<IApplicationInstanceInfo>();
        applicationInstanceInfo.ApplicationName.Should().NotBeNull();

        hostWrapper.Services.GetServices<IDynamicMessageProcessor>().OfType<TracingLogProcessor>().Should().ContainSingle();
    }

    private static class HostWrapperFactory
    {
        public static HostWrapper GetForOnly(string assemblyNameToInclude, HostBuilderType hostBuilderType)
        {
            IReadOnlySet<string> exclusions = SteeltoeAssemblyNames.Only(assemblyNameToInclude);
            return GetExcluding(exclusions, hostBuilderType);
        }

        public static HostWrapper GetExcluding(IReadOnlySet<string> assemblyNamesToExclude, HostBuilderType hostBuilderType)
        {
            return hostBuilderType switch
            {
                HostBuilderType.Host => HostWrapper.Wrap(GetExcludingFromHostBuilder(assemblyNamesToExclude)),
                HostBuilderType.WebHost => HostWrapper.Wrap(GetExcludingFromWebHostBuilder(assemblyNamesToExclude)),
                HostBuilderType.WebApplication => HostWrapper.Wrap(GetExcludingFromWebApplicationBuilder(assemblyNamesToExclude)),
                HostBuilderType.HostApplication => HostWrapper.Wrap(GetExcludingFromHostApplicationBuilder(assemblyNamesToExclude)),
                _ => throw new NotSupportedException()
            };
        }

        private static IHost GetExcludingFromHostBuilder(IReadOnlySet<string> assemblyNamesToExclude)
        {
            HostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();

            hostBuilder.ConfigureWebHost(builder =>
            {
                builder.ConfigureAppConfiguration(configurationBuilder =>
                {
                    configurationBuilder.Add(FastTestConfigurations.All);
                    configurationBuilder.AddInMemoryCollection(HealthProbeSettings);
                });

                builder.Configure(applicationBuilder => applicationBuilder.UseRouting());
                builder.AddSteeltoe(assemblyNamesToExclude);
            });

            return hostBuilder.Build();
        }

        private static IWebHost GetExcludingFromWebHostBuilder(IReadOnlySet<string> assemblyNamesToExclude)
        {
            WebHostBuilder builder = TestWebHostBuilderFactory.Create();

            builder.ConfigureAppConfiguration(configurationBuilder =>
            {
                configurationBuilder.Add(FastTestConfigurations.All);
                configurationBuilder.AddInMemoryCollection(HealthProbeSettings);
            });

            builder.ConfigureServices(services => services.AddRouting());
            builder.Configure(applicationBuilder => applicationBuilder.UseRouting());
            builder.AddSteeltoe(assemblyNamesToExclude);

            return builder.Build();
        }

        private static WebApplication GetExcludingFromWebApplicationBuilder(IReadOnlySet<string> assemblyNamesToExclude)
        {
            WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault();
            builder.Configuration.Add(FastTestConfigurations.All);
            builder.Configuration.AddInMemoryCollection(HealthProbeSettings);
            builder.AddSteeltoe(assemblyNamesToExclude);

            WebApplication host = builder.Build();
            host.UseRouting();

            return host;
        }

        private static IHost GetExcludingFromHostApplicationBuilder(IReadOnlySet<string> assemblyNamesToExclude)
        {
            HostApplicationBuilder builder = TestHostApplicationBuilderFactory.Create();
            builder.Configuration.Add(FastTestConfigurations.All);
            builder.Configuration.AddInMemoryCollection(HealthProbeSettings);
            builder.AddSteeltoe(assemblyNamesToExclude);

            return builder.Build();
        }
    }
}
