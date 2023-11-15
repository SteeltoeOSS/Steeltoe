// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
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
using Steeltoe.Common;
using Steeltoe.Common.Options;
using Steeltoe.Common.Security;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Steeltoe.Configuration.ConfigServer;
using Steeltoe.Configuration.Kubernetes;
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
using Steeltoe.Discovery;
using Steeltoe.Discovery.Eureka;
using Steeltoe.Logging;
using Steeltoe.Logging.DynamicSerilog;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Web.Hypermedia;
using Steeltoe.Management.Wavefront.Exporters;
using Xunit;

namespace Steeltoe.Bootstrap.AutoConfiguration.Test;

public sealed class HostBuilderExtensionsTest
{
    private readonly Action<IWebHostBuilder> _testServerWithRouting = builder =>
        builder.UseTestServer().ConfigureServices(s => s.AddRouting().AddActionDescriptorCollectionProvider()).Configure(a => a.UseRouting());

    [Fact]
    public void ConfigServerConfiguration_IsAutowired()
    {
        string[] exclusions = SteeltoeAssemblyNames.All.Except(new[]
        {
            SteeltoeAssemblyNames.ConfigurationConfigServer
        }).ToArray();

        IHostBuilder hostBuilder = new HostBuilder().ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(TestHelpers.FastTestsConfiguration));

        IHost host = hostBuilder.AddSteeltoe(exclusions).Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        Assert.NotNull(configuration.FindConfigurationProvider<CloudFoundryConfigurationProvider>());
        Assert.NotNull(configuration.FindConfigurationProvider<ConfigServerConfigurationProvider>());
    }

    [Fact]
    public void CloudFoundryConfiguration_IsAutowired()
    {
        string[] exclusions = SteeltoeAssemblyNames.All.Except(new[]
        {
            SteeltoeAssemblyNames.ConfigurationCloudFoundry
        }).ToArray();

        IHostBuilder hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddSteeltoe(exclusions).Build();
        var configurationRoot = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();

        Assert.Single(configurationRoot.Providers.OfType<CloudFoundryConfigurationProvider>());
    }

    [Fact(Skip = "Requires Kubernetes")]
    public void KubernetesConfiguration_IsAutowired()
    {
        using var scope = new EnvironmentVariableScope("KUBERNETES_SERVICE_HOST", "TEST");

        string[] exclusions = SteeltoeAssemblyNames.All.Except(new[]
        {
            SteeltoeAssemblyNames.ConfigurationKubernetes
        }).ToArray();

        IHostBuilder hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddSteeltoe(exclusions).Build();
        var configurationRoot = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();

        Assert.Equal(2, configurationRoot.Providers.OfType<KubernetesConfigMapProvider>().Count());
        Assert.Equal(2, configurationRoot.Providers.OfType<KubernetesSecretProvider>().Count());
    }

    [Fact]
    public void RandomValueConfiguration_IsAutowired()
    {
        string[] exclusions = SteeltoeAssemblyNames.All.Except(new[]
        {
            SteeltoeAssemblyNames.ConfigurationRandomValue
        }).ToArray();

        IHostBuilder hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddSteeltoe(exclusions).Build();
        var configurationRoot = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();

        Assert.Single(configurationRoot.Providers.OfType<RandomValueProvider>());
    }

    [Fact]
    public void PlaceholderResolver_IsAutowired()
    {
        string[] exclusions = SteeltoeAssemblyNames.All.Except(new[]
        {
            SteeltoeAssemblyNames.ConfigurationPlaceholder
        }).ToArray();

        IHostBuilder hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddSteeltoe(exclusions).Build();
        var configurationRoot = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();

        Assert.Single(configurationRoot.Providers);
        Assert.Single(configurationRoot.Providers.OfType<PlaceholderResolverProvider>());
    }

    [Fact]
    public void Connectors_AreAutowired()
    {
        string[] exclusions = SteeltoeAssemblyNames.All.Except(new[]
        {
            SteeltoeAssemblyNames.Connectors
        }).ToArray();

        IHostBuilder hostBuilder = new HostBuilder();
        hostBuilder.AddSteeltoe(exclusions);

        IHost host = hostBuilder.Build();
        var configurationRoot = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();

        configurationRoot.Providers.Should().ContainSingle(provider => provider is KubernetesServiceBindingConfigurationProvider);
        configurationRoot.Providers.Should().ContainSingle(provider => provider is CloudFoundryServiceBindingConfigurationProvider);

        host.Services.GetRequiredService<ConnectorFactory<CosmosDbOptions, CosmosClient>>().Should().NotBeNull();
        host.Services.GetRequiredService<ConnectorFactory<MongoDbOptions, IMongoClient>>().Should().NotBeNull();
        host.Services.GetRequiredService<ConnectorFactory<MySqlOptions, MySqlConnection>>().Should().NotBeNull();
        host.Services.GetRequiredService<ConnectorFactory<PostgreSqlOptions, NpgsqlConnection>>().Should().NotBeNull();
        host.Services.GetRequiredService<ConnectorFactory<RabbitMQOptions, IConnection>>().Should().NotBeNull();
        host.Services.GetRequiredService<ConnectorFactory<RedisOptions, IConnectionMultiplexer>>().Should().NotBeNull();
        host.Services.GetRequiredService<ConnectorFactory<RedisOptions, IDistributedCache>>().Should().NotBeNull();
        host.Services.GetRequiredService<ConnectorFactory<SqlServerOptions, SqlConnection>>().Should().NotBeNull();
    }

    [Fact]
    public void SqlServerConnector_NotAutowiredIfExcluded()
    {
        var exclusions = new HashSet<string>(SteeltoeAssemblyNames.All);
        exclusions.Remove(SteeltoeAssemblyNames.Connectors);
        exclusions.Add("Microsoft.Data.SqlClient");
        exclusions.Add("System.Data.SqlClient");

        IHostBuilder hostBuilder = new HostBuilder();
        hostBuilder.AddSteeltoe(exclusions);

        IHost host = hostBuilder.Build();

        host.Services.GetService<ConnectorFactory<SqlServerOptions, SqlConnection>>().Should().BeNull();
    }

    [Fact]
    public void DynamicSerilog_IsAutowired()
    {
        string[] exclusions = SteeltoeAssemblyNames.All.Except(new[]
        {
            SteeltoeAssemblyNames.LoggingDynamicSerilog
        }).ToArray();

        IHostBuilder hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddSteeltoe(exclusions).Build();

        var loggerProvider = host.Services.GetRequiredService<IDynamicLoggerProvider>();

        Assert.IsType<DynamicSerilogLoggerProvider>(loggerProvider);
    }

    [Fact]
    public void ServiceDiscovery_IsAutowired()
    {
        string[] exclusions = SteeltoeAssemblyNames.All.Except(new[]
        {
            SteeltoeAssemblyNames.DiscoveryClient
        }).ToArray();

        IHostBuilder hostBuilder = new HostBuilder().ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(TestHelpers.FastTestsConfiguration));

        IHost host = hostBuilder.AddSteeltoe(exclusions).Build();
        IDiscoveryClient[] discoveryClients = host.Services.GetServices<IDiscoveryClient>().ToArray();

        Assert.Single(discoveryClients);
        Assert.IsType<EurekaDiscoveryClient>(discoveryClients[0]);
    }

    [Fact]
    public void WavefrontMetricsExporter_IsAutowired()
    {
        string[] exclusions = SteeltoeAssemblyNames.All.Except(new[]
        {
            SteeltoeAssemblyNames.ManagementWavefront
        }).ToArray();

        IHost host = new HostBuilder().ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(TestHelpers.WavefrontConfiguration))
            .AddSteeltoe(exclusions).Build();

        var meterProvider = host.Services.GetRequiredService<MeterProvider>();
        Assert.NotNull(meterProvider);
    }

    [Fact]
    public void WavefrontTraceExporter_IsAutowired()
    {
        string[] exclusions = SteeltoeAssemblyNames.All.Except(new[]
        {
            SteeltoeAssemblyNames.ManagementTracing
        }).ToArray();

        IHost host = new HostBuilder().ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(TestHelpers.WavefrontConfiguration))
            .AddSteeltoe(exclusions).Build();

        var tracerProvider = host.Services.GetRequiredService<TracerProvider>();
        Assert.NotNull(tracerProvider);

        PropertyInfo processorProperty =
            tracerProvider.GetType().GetProperty("Processor", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(processorProperty);

        object processor = processorProperty.GetValue(tracerProvider);
        Assert.NotNull(processor);

        FieldInfo exporterField = processor.GetType().GetField("exporter", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(exporterField);

        object exporter = exporterField.GetValue(processor);
        Assert.NotNull(exporter);
        Assert.IsType<WavefrontTraceExporter>(exporter);
    }

    [Fact]
    public async Task KubernetesActuators_AreAutowired()
    {
        string[] exclusions = SteeltoeAssemblyNames.All.Except(new[]
        {
            SteeltoeAssemblyNames.ManagementKubernetes
        }).ToArray();

        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        IHost host = await hostBuilder.AddSteeltoe(exclusions).StartAsync();
        HttpClient testClient = host.GetTestServer().CreateClient();

        HttpResponseMessage response = await testClient.GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await testClient.GetAsync(new Uri("/actuator/info", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await testClient.GetAsync(new Uri("/actuator/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await testClient.GetAsync(new Uri("/actuator/health/liveness", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"LivenessState\":\"CORRECT\"", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        response = await testClient.GetAsync(new Uri("/actuator/health/readiness", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"ReadinessState\":\"ACCEPTING_TRAFFIC\"", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public void AllActuators_AreAutowired()
    {
        string[] exclusions = SteeltoeAssemblyNames.All.Except(new[]
        {
            SteeltoeAssemblyNames.ManagementEndpoint
        }).ToArray();

        IHostBuilder hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddSteeltoe(exclusions).Build();
        IEnumerable<IActuatorEndpointHandler> handlers = host.Services.GetServices<IActuatorEndpointHandler>();
        Assert.Single(handlers);

        var filter = host.Services.GetRequiredService<IStartupFilter>();
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public void Tracing_IsAutowired()
    {
        string[] exclusions = SteeltoeAssemblyNames.All.Except(new[]
        {
            SteeltoeAssemblyNames.ManagementTracing
        }).ToArray();

        IHostBuilder hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddSteeltoe(exclusions).Build();
        var tracerProvider = host.Services.GetRequiredService<TracerProvider>();

        Assert.NotNull(host.Services.GetRequiredService<IHostedService>());
        Assert.NotNull(host.Services.GetRequiredService<ITracingOptions>());
        Assert.NotNull(host.Services.GetRequiredService<IDynamicMessageProcessor>());

        // confirm instrumentation(s) were added as expected
        FieldInfo instrumentationsField = tracerProvider.GetType().GetField("instrumentations", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(instrumentationsField);

        var instrumentations = (List<object>)instrumentationsField.GetValue(tracerProvider);
        Assert.NotNull(instrumentations);
        Assert.Equal(2, instrumentations.Count);
        Assert.Contains(instrumentations, obj => obj.GetType().Name.Contains("Http", StringComparison.Ordinal));
        Assert.Contains(instrumentations, obj => obj.GetType().Name.Contains("AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void CloudFoundryContainerSecurity_IsAutowired()
    {
        string[] exclusions = SteeltoeAssemblyNames.All.Except(new[]
        {
            SteeltoeAssemblyNames.SecurityAuthenticationCloudFoundry
        }).ToArray();

        IHostBuilder hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddSteeltoe(exclusions).Build();
        var configurationRoot = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();

        Assert.Single(configurationRoot.Providers.OfType<PemCertificateProvider>());
        Assert.NotNull(host.Services.GetRequiredService<IOptions<CertificateOptions>>());
        Assert.NotNull(host.Services.GetRequiredService<ICertificateRotationService>());
        Assert.NotNull(host.Services.GetRequiredService<IAuthorizationHandler>());
    }
}
