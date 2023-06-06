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
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Wavefront.Exporters;
using Xunit;

namespace Steeltoe.Bootstrap.AutoConfiguration.Test;

public class WebHostBuilderExtensionsTest
{
    private readonly IWebHostBuilder _testServerWithRouting =
        new WebHostBuilder().UseTestServer().ConfigureServices(s => s.AddRouting()).Configure(a => a.UseRouting());

    [Fact]
    public void ConfigServerConfiguration_IsAutowired()
    {
        IEnumerable<string> exclusions = SteeltoeAssemblies.AllAssemblies.Except(new List<string>
        {
            SteeltoeAssemblies.SteeltoeConfigurationConfigServer,
            SteeltoeAssemblies.SteeltoeConfigurationCloudFoundry
        });

        IWebHostBuilder hostBuilder = new WebHostBuilder()
            .ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(TestHelpers.FastTestsConfiguration)).Configure(_ =>
            {
            });

        IWebHost host = hostBuilder.AddSteeltoe(exclusions).Build();
        var configurationRoot = host.Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

        Assert.Single(configurationRoot.Providers.OfType<CloudFoundryConfigurationProvider>());
        Assert.Single(configurationRoot.Providers.OfType<ConfigServerConfigurationProvider>());
    }

    [Fact]
    public void CloudFoundryConfiguration_IsAutowired()
    {
        IEnumerable<string> exclusions = SteeltoeAssemblies.AllAssemblies.Except(new List<string>
        {
            SteeltoeAssemblies.SteeltoeConfigurationCloudFoundry
        });

        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddSteeltoe(exclusions).Build();
        var configurationRoot = host.Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

        Assert.Single(configurationRoot.Providers.OfType<CloudFoundryConfigurationProvider>());
    }

    [Fact(Skip = "Requires Kubernetes")]
    public void KubernetesConfiguration_IsAutowired()
    {
        using var scope = new EnvironmentVariableScope("KUBERNETES_SERVICE_HOST", "TEST");

        IEnumerable<string> exclusions = SteeltoeAssemblies.AllAssemblies.Except(new List<string>
        {
            SteeltoeAssemblies.SteeltoeConfigurationKubernetes
        });

        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddSteeltoe(exclusions).Build();
        var configurationRoot = host.Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

        Assert.Equal(2, configurationRoot.Providers.OfType<KubernetesConfigMapProvider>().Count());
        Assert.Equal(2, configurationRoot.Providers.OfType<KubernetesSecretProvider>().Count());
    }

    [Fact]
    public void RandomValueConfiguration_IsAutowired()
    {
        List<string> exclusions = SteeltoeAssemblies.AllAssemblies.Except(new List<string>
        {
            SteeltoeAssemblies.SteeltoeConfigurationRandomValue
        }).ToList();

        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddSteeltoe(exclusions).Build();
        var configurationRoot = host.Services.GetService<IConfiguration>() as ConfigurationRoot;

        Assert.Single(configurationRoot.Providers.OfType<RandomValueProvider>());
    }

    [Fact]
    public void PlaceholderResolver_IsAutowired()
    {
        IEnumerable<string> exclusions = SteeltoeAssemblies.AllAssemblies.Except(new List<string>
        {
            SteeltoeAssemblies.SteeltoeConfigurationPlaceholder
        });

        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddSteeltoe(exclusions).Build();
        var configurationRoot = host.Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

        Assert.Single(configurationRoot.Providers);
        Assert.Single(configurationRoot.Providers.OfType<PlaceholderResolverProvider>());
    }

    [Fact]
    public void Connectors_AreAutowired()
    {
        string[] exclusions = SteeltoeAssemblies.AllAssemblies.Except(new[]
        {
            SteeltoeAssemblies.SteeltoeConnectors
        }).ToArray();

        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        hostBuilder.AddSteeltoe(exclusions);

        IWebHost host = hostBuilder.Build();
        var configurationRoot = (ConfigurationRoot)host.Services.GetService<IConfiguration>();

        configurationRoot.Providers.Should().ContainSingle(provider => provider is KubernetesServiceBindingConfigurationProvider);
        configurationRoot.Providers.Should().ContainSingle(provider => provider is CloudFoundryServiceBindingConfigurationProvider);

        host.Services.GetService<ConnectorFactory<CosmosDbOptions, CosmosClient>>().Should().NotBeNull();
        host.Services.GetService<ConnectorFactory<MongoDbOptions, IMongoClient>>().Should().NotBeNull();
        host.Services.GetService<ConnectorFactory<MySqlOptions, MySqlConnection>>().Should().NotBeNull();
        host.Services.GetService<ConnectorFactory<PostgreSqlOptions, NpgsqlConnection>>().Should().NotBeNull();
        host.Services.GetService<ConnectorFactory<RabbitMQOptions, IConnection>>().Should().NotBeNull();
        host.Services.GetService<ConnectorFactory<RedisOptions, IConnectionMultiplexer>>().Should().NotBeNull();
        host.Services.GetService<ConnectorFactory<RedisOptions, IDistributedCache>>().Should().NotBeNull();
        host.Services.GetService<ConnectorFactory<SqlServerOptions, SqlConnection>>().Should().NotBeNull();
    }

    [Fact]
    public void DynamicSerilog_IsAutowired()
    {
        IEnumerable<string> exclusions = SteeltoeAssemblies.AllAssemblies.Except(new List<string>
        {
            SteeltoeAssemblies.SteeltoeLoggingDynamicSerilog
        });

        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddSteeltoe(exclusions).Build();

        var loggerProvider = (IDynamicLoggerProvider)host.Services.GetService(typeof(IDynamicLoggerProvider));

        Assert.IsType<SerilogDynamicProvider>(loggerProvider);
    }

    [Fact]
    public void ServiceDiscovery_IsAutowired()
    {
        IEnumerable<string> exclusions = SteeltoeAssemblies.AllAssemblies.Except(new List<string>
        {
            SteeltoeAssemblies.SteeltoeDiscoveryClient
        });

        IWebHostBuilder hostBuilder = new WebHostBuilder()
            .ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(TestHelpers.FastTestsConfiguration)).Configure(_ =>
            {
            });

        IWebHost host = hostBuilder.AddSteeltoe(exclusions).Build();
        IEnumerable<IDiscoveryClient> discoveryClient = host.Services.GetServices<IDiscoveryClient>();

        Assert.Single(discoveryClient);
        Assert.IsType<EurekaDiscoveryClient>(discoveryClient.First());
    }

    [Fact]
    public void WavefrontMetricsExporter_IsAutowired()
    {
        IEnumerable<string> exclusions = SteeltoeAssemblies.AllAssemblies.Except(new List<string>
        {
            SteeltoeAssemblies.SteeltoeWavefront
        });

        IWebHost host = new WebHostBuilder().ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(TestHelpers.WavefrontConfiguration))
            .AddSteeltoe(exclusions).Configure(_ =>
            {
            }).Build();

        var meterProvider = host.Services.GetService<MeterProvider>();

        Assert.NotNull(meterProvider);
    }

    [Fact]
    public void WavefrontTraceExporter_IsAutowired()
    {
        IEnumerable<string> exclusions = SteeltoeAssemblies.AllAssemblies.Except(new List<string>
        {
            SteeltoeAssemblies.SteeltoeManagementTracing
        });

        IWebHost host = new WebHostBuilder().ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(TestHelpers.WavefrontConfiguration))
            .AddSteeltoe(exclusions).Configure(_ =>
            {
            }).Build();

        var tracerProvider = host.Services.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);

        object processor = tracerProvider.GetType().GetProperty("Processor", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(tracerProvider);

        object exporter = processor.GetType().GetField("exporter", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(processor);
        Assert.NotNull(exporter);
        Assert.IsType<WavefrontTraceExporter>(exporter);
    }

    [Fact]
    public async Task KubernetesActuators_AreAutowired()
    {
        IEnumerable<string> exclusions = SteeltoeAssemblies.AllAssemblies.Except(new List<string>
        {
            SteeltoeAssemblies.SteeltoeManagementKubernetes
        });

        IWebHostBuilder hostBuilder = _testServerWithRouting;

        IWebHost host = hostBuilder.AddSteeltoe(exclusions).Start();
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
        IEnumerable<string> exclusions = SteeltoeAssemblies.AllAssemblies.Except(new List<string>
        {
            SteeltoeAssemblies.SteeltoeManagementEndpoint
        });

        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddSteeltoe(exclusions).Build();
        IEnumerable<IActuatorEndpoint> managementEndpoint = host.Services.GetServices<IActuatorEndpoint>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public void Tracing_IsAutowired()
    {
        IEnumerable<string> exclusions = SteeltoeAssemblies.AllAssemblies.Except(new List<string>
        {
            SteeltoeAssemblies.SteeltoeManagementTracing
        });

        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddSteeltoe(exclusions).Build();
        var tracerProvider = host.Services.GetService<TracerProvider>();

        Assert.NotNull(host.Services.GetService<IHostedService>());
        Assert.NotNull(host.Services.GetService<ITracingOptions>());
        Assert.NotNull(tracerProvider);
        Assert.NotNull(host.Services.GetService<IDynamicMessageProcessor>());

        // confirm instrumentation(s) were added as expected
        var instrumentations =
            tracerProvider.GetType().GetField("instrumentations", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tracerProvider) as List<object>;

        Assert.NotNull(instrumentations);
        Assert.Equal(2, instrumentations.Count);
        Assert.Contains(instrumentations, obj => obj.GetType().Name.Contains("Http", StringComparison.Ordinal));
        Assert.Contains(instrumentations, obj => obj.GetType().Name.Contains("AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void CloudFoundryContainerSecurity_IsAutowired()
    {
        IEnumerable<string> exclusions = SteeltoeAssemblies.AllAssemblies.Except(new List<string>
        {
            SteeltoeAssemblies.SteeltoeSecurityAuthenticationCloudFoundry
        });

        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddSteeltoe(exclusions).Build();
        var configurationRoot = host.Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

        Assert.Single(configurationRoot.Providers.OfType<PemCertificateProvider>());
        Assert.NotNull(host.Services.GetService<IOptions<CertificateOptions>>());
        Assert.NotNull(host.Services.GetService<ICertificateRotationService>());
        Assert.NotNull(host.Services.GetService<IAuthorizationHandler>());
    }
}
