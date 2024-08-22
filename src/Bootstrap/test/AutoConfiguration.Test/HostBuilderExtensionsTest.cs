// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Azure.Cosmos;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MySqlConnector;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using RabbitMQ.Client;
using StackExchange.Redis;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Steeltoe.Configuration.ConfigServer;
using Steeltoe.Configuration.Kubernetes.ServiceBinding;
using Steeltoe.Configuration.Placeholder;
using Steeltoe.Configuration.RandomValue;
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
using Steeltoe.Logging.DynamicSerilog;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Tracing;
using Steeltoe.Management.Wavefront.Exporters;

namespace Steeltoe.Bootstrap.AutoConfiguration.Test;

public sealed class HostBuilderExtensionsTest
{
    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task ConfigServerConfiguration_IsAutowired(HostBuilderType hostBuilderType)
    {
        await using HostWrapper hostWrapper = HostWrapperFactory.GetForOnly(SteeltoeAssemblyNames.ConfigurationConfigServer, hostBuilderType);

        AssertConfigServerConfigurationIsAutowired(hostWrapper);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task CloudFoundryConfiguration_IsAutowired(HostBuilderType hostBuilderType)
    {
        await using HostWrapper hostWrapper = HostWrapperFactory.GetForOnly(SteeltoeAssemblyNames.ConfigurationCloudFoundry, hostBuilderType);

        AssertCloudFoundryConfigurationIsAutowired(hostWrapper);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task RandomValueConfiguration_IsAutowired(HostBuilderType hostBuilderType)
    {
        await using HostWrapper hostWrapper = HostWrapperFactory.GetForOnly(SteeltoeAssemblyNames.ConfigurationRandomValue, hostBuilderType);

        AssertRandomValueConfigurationIsAutowired(hostWrapper);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task PlaceholderResolver_IsAutowired(HostBuilderType hostBuilderType)
    {
        await using HostWrapper hostWrapper = HostWrapperFactory.GetForOnly(SteeltoeAssemblyNames.ConfigurationPlaceholder, hostBuilderType);

        AssertPlaceholderResolverIsAutowired(hostWrapper);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task Connectors_AreAutowired(HostBuilderType hostBuilderType)
    {
        await using HostWrapper hostWrapper = HostWrapperFactory.GetForOnly(SteeltoeAssemblyNames.Connectors, hostBuilderType);

        AssertConnectorsAreAutowired(hostWrapper);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task SqlServerConnector_NotAutowiredIfDependenciesExcluded(HostBuilderType hostBuilderType)
    {
        var exclusions = new HashSet<string>(SteeltoeAssemblyNames.All);
        exclusions.Remove(SteeltoeAssemblyNames.Connectors);
        exclusions.Add("Microsoft.Data.SqlClient");
        exclusions.Add("System.Data.SqlClient");

        await using HostWrapper hostWrapper = HostWrapperFactory.GetExcluding(exclusions, hostBuilderType);

        hostWrapper.Services.GetService<ConnectorFactory<SqlServerOptions, SqlConnection>>().Should().BeNull();
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task DynamicSerilog_IsAutowired(HostBuilderType hostBuilderType)
    {
        await using HostWrapper hostWrapper = HostWrapperFactory.GetForOnly(SteeltoeAssemblyNames.LoggingDynamicSerilog, hostBuilderType);

        AssertDynamicSerilogIsAutowired(hostWrapper);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
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

        AssertServiceDiscoveryClientsAreAutowired(hostWrapper);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task Prometheus_IsAutowired(HostBuilderType hostBuilderType)
    {
        await using HostWrapper hostWrapper = HostWrapperFactory.GetForOnly(SteeltoeAssemblyNames.ManagementPrometheus, hostBuilderType);

        AssertPrometheusIsAutowired(hostWrapper);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task WavefrontMetricsExporter_IsAutowired(HostBuilderType hostBuilderType)
    {
        await using HostWrapper hostWrapper = HostWrapperFactory.GetForOnly(SteeltoeAssemblyNames.ManagementWavefront, hostBuilderType);

        AssertWavefrontMetricsExporterIsAutowired(hostWrapper);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task WavefrontTraceExporter_IsAutowired(HostBuilderType hostBuilderType)
    {
        await using HostWrapper hostWrapper = HostWrapperFactory.GetForOnly(SteeltoeAssemblyNames.ManagementTracing, hostBuilderType);

        AssertWavefrontTraceExporterIsAutowired(hostWrapper);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task AllActuators_AreAutowired(HostBuilderType hostBuilderType)
    {
        await using HostWrapper hostWrapper = HostWrapperFactory.GetForOnly(SteeltoeAssemblyNames.ManagementEndpoint, hostBuilderType);
        await hostWrapper.StartAsync();

        await AssertAllActuatorsAreAutowiredAsync(hostWrapper, true);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task Tracing_IsAutowired(HostBuilderType hostBuilderType)
    {
        await using HostWrapper hostWrapper = HostWrapperFactory.GetForOnly(SteeltoeAssemblyNames.ManagementTracing, hostBuilderType);

        AssertTracingIsAutowired(hostWrapper);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task Everything_IsAutowired(HostBuilderType hostBuilderType)
    {
        await using HostWrapper hostWrapper = HostWrapperFactory.GetExcluding(new HashSet<string>(), hostBuilderType);

        AssertConfigServerConfigurationIsAutowired(hostWrapper);
        AssertCloudFoundryConfigurationIsAutowired(hostWrapper);
        AssertRandomValueConfigurationIsAutowired(hostWrapper);
        AssertPlaceholderResolverIsAutowired(hostWrapper);
        AssertConnectorsAreAutowired(hostWrapper);
        AssertDynamicSerilogIsAutowired(hostWrapper);
        AssertServiceDiscoveryClientsAreAutowired(hostWrapper);
        AssertPrometheusIsAutowired(hostWrapper);
        AssertWavefrontMetricsExporterIsAutowired(hostWrapper);
        AssertWavefrontTraceExporterIsAutowired(hostWrapper);
        AssertTracingIsAutowired(hostWrapper);

        await hostWrapper.StartAsync();

        await AssertAllActuatorsAreAutowiredAsync(hostWrapper, false);
    }

    private static void AssertConfigServerConfigurationIsAutowired(HostWrapper hostWrapper)
    {
        var configuration = hostWrapper.Services.GetRequiredService<IConfiguration>();

        configuration.FindConfigurationProvider<CloudFoundryConfigurationProvider>().Should().NotBeNull();
        configuration.FindConfigurationProvider<ConfigServerConfigurationProvider>().Should().NotBeNull();
    }

    private static void AssertCloudFoundryConfigurationIsAutowired(HostWrapper hostWrapper)
    {
        var configuration = hostWrapper.Services.GetRequiredService<IConfiguration>();

        configuration.FindConfigurationProvider<CloudFoundryConfigurationProvider>().Should().NotBeNull();
    }

    private static void AssertRandomValueConfigurationIsAutowired(HostWrapper hostWrapper)
    {
        var configuration = hostWrapper.Services.GetRequiredService<IConfiguration>();

        configuration.FindConfigurationProvider<RandomValueProvider>().Should().NotBeNull();
    }

    private static void AssertPlaceholderResolverIsAutowired(HostWrapper hostWrapper)
    {
        var configurationRoot = (IConfigurationRoot)hostWrapper.Services.GetRequiredService<IConfiguration>();

        configurationRoot.Providers.OfType<PlaceholderResolverProvider>().Should().HaveCount(1);
    }

    private static void AssertConnectorsAreAutowired(HostWrapper hostWrapper)
    {
        var configuration = hostWrapper.Services.GetRequiredService<IConfiguration>();

        configuration.FindConfigurationProvider<KubernetesServiceBindingConfigurationProvider>().Should().NotBeNull();
        configuration.FindConfigurationProvider<CloudFoundryServiceBindingConfigurationProvider>().Should().NotBeNull();

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

    private static void AssertServiceDiscoveryClientsAreAutowired(HostWrapper hostWrapper)
    {
        IDiscoveryClient[] discoveryClients = hostWrapper.Services.GetServices<IDiscoveryClient>().ToArray();

        discoveryClients.Should().HaveCount(3);
        discoveryClients.Should().ContainSingle(discoveryClient => discoveryClient is ConfigurationDiscoveryClient);
        discoveryClients.Should().ContainSingle(discoveryClient => discoveryClient is ConsulDiscoveryClient);
        discoveryClients.Should().ContainSingle(discoveryClient => discoveryClient is EurekaDiscoveryClient);
    }

    private static void AssertPrometheusIsAutowired(HostWrapper hostWrapper)
    {
        hostWrapper.Services.GetService<MeterProvider>().Should().NotBeNull();
    }

    private static void AssertWavefrontMetricsExporterIsAutowired(HostWrapper hostWrapper)
    {
        hostWrapper.Services.GetService<MeterProvider>().Should().NotBeNull();
    }

    private static void AssertWavefrontTraceExporterIsAutowired(HostWrapper hostWrapper)
    {
        var tracerProvider = hostWrapper.Services.GetRequiredService<TracerProvider>();

        PropertyInfo? processorProperty = tracerProvider.GetType().GetProperty("Processor", BindingFlags.NonPublic | BindingFlags.Instance);
        processorProperty.Should().NotBeNull();

        object? processor = processorProperty!.GetValue(tracerProvider);
        processor.Should().NotBeNull();

        FieldInfo? exporterField = processor!.GetType().GetField("exporter", BindingFlags.NonPublic | BindingFlags.Instance);
        exporterField.Should().NotBeNull();

        object? exporter = exporterField!.GetValue(processor);
        exporter.Should().BeOfType<WavefrontTraceExporter>();
    }

    private static async Task AssertAllActuatorsAreAutowiredAsync(HostWrapper hostWrapper, bool expectHealthy)
    {
        hostWrapper.Services.GetServices<IActuatorEndpointHandler>().Should().HaveCount(1);
        hostWrapper.Services.GetServices<IStartupFilter>().OfType<AllActuatorsStartupFilter>().Should().HaveCount(1);

        using HttpClient httpClient = hostWrapper.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("/actuator", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response = await httpClient.GetAsync(new Uri("/actuator/info", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response = await httpClient.GetAsync(new Uri("/actuator/health", UriKind.Relative));
        response.StatusCode.Should().Be(expectHealthy ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable);

        response = await httpClient.GetAsync(new Uri("/actuator/health/liveness", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("""LivenessState":"CORRECT""");

        response = await httpClient.GetAsync(new Uri("/actuator/health/readiness", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("""ReadinessState":"ACCEPTING_TRAFFIC""");
    }

    private static void AssertTracingIsAutowired(HostWrapper hostWrapper)
    {
        var tracerProvider = hostWrapper.Services.GetRequiredService<TracerProvider>();

        IHostedService[] hostedServices = hostWrapper.Services.GetServices<IHostedService>().ToArray();
        hostedServices.Should().ContainSingle(hostedService => hostedService.GetType().Name == "TelemetryHostedService");

        var optionsMonitor = hostWrapper.Services.GetRequiredService<IOptionsMonitor<TracingOptions>>();
        optionsMonitor.CurrentValue.Name.Should().NotBeNull();

        hostWrapper.Services.GetService<IDynamicMessageProcessor>().Should().NotBeNull();

        FieldInfo? instrumentationsField = tracerProvider.GetType().GetField("instrumentations", BindingFlags.NonPublic | BindingFlags.Instance);
        instrumentationsField.Should().NotBeNull();

        var instrumentations = (List<object>?)instrumentationsField!.GetValue(tracerProvider);

        instrumentations.Should().HaveCount(2);
        instrumentations.Should().ContainSingle(instance => instance.GetType().Name == "HttpClientInstrumentation");
        instrumentations.Should().ContainSingle(instance => instance.GetType().Name == "AspNetCoreInstrumentation");
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
                HostBuilderType.Host => HostWrapper.Wrap(GetHostExcluding(assemblyNamesToExclude)),
                HostBuilderType.WebHost => HostWrapper.Wrap(GetWebHostExcluding(assemblyNamesToExclude)),
                HostBuilderType.WebApplication => HostWrapper.Wrap(GetWebApplicationExcluding(assemblyNamesToExclude)),
                _ => throw new NotSupportedException()
            };
        }

        private static IHost GetHostExcluding(IReadOnlySet<string> assemblyNamesToExclude)
        {
            IHostBuilder hostBuilder = TestHostBuilderFactory.Create();

            hostBuilder.ConfigureWebHost(builder =>
            {
                builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.Add(FastTestConfigurations.All));
                builder.Configure(applicationBuilder => applicationBuilder.UseRouting());
                builder.UseTestServer();

                builder.AddSteeltoe(assemblyNamesToExclude);
            });

            return hostBuilder.Build();
        }

        private static IWebHost GetWebHostExcluding(IReadOnlySet<string> assemblyNamesToExclude)
        {
            IWebHostBuilder builder = TestWebHostBuilderFactory.Create();
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.Add(FastTestConfigurations.All));
            builder.Configure(applicationBuilder => applicationBuilder.UseRouting());
            builder.UseTestServer();

            builder.AddSteeltoe(assemblyNamesToExclude);

            return builder.Build();
        }

        private static WebApplication GetWebApplicationExcluding(IReadOnlySet<string> assemblyNamesToExclude)
        {
            WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault();
            builder.Configuration.Add(FastTestConfigurations.All);
            builder.WebHost.UseTestServer();

            builder.AddSteeltoe(assemblyNamesToExclude);

            WebApplication host = builder.Build();
            host.UseRouting();

            return host;
        }
    }
}
