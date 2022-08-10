// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.SqlClient;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MySql.Data.MySqlClient;
using Npgsql;
using OpenTelemetry.Trace;
using Oracle.ManagedDataAccess.Client;
using RabbitMQ.Client;
using StackExchange.Redis;
using Steeltoe.Common.Options;
using Steeltoe.Common.Security;
using Steeltoe.Connector;
using Steeltoe.Discovery;
using Steeltoe.Discovery.Eureka;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Steeltoe.Extensions.Configuration.ConfigServer;
using Steeltoe.Extensions.Configuration.Kubernetes;
using Steeltoe.Extensions.Configuration.Placeholder;
using Steeltoe.Extensions.Configuration.RandomValue;
using Steeltoe.Extensions.Logging;
using Steeltoe.Extensions.Logging.DynamicSerilog;
using Steeltoe.Management;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.OpenTelemetry.Exporters;
using Steeltoe.Management.OpenTelemetry.Trace;
using Xunit;

namespace Steeltoe.Bootstrap.Autoconfig.Test;

public class WebHostBuilderExtensionsTest
{
    private readonly IWebHostBuilder _testServerWithRouting =
        new WebHostBuilder().UseTestServer().ConfigureServices(s => s.AddRouting()).Configure(a => a.UseRouting());

    [Fact]
    public void ConfigServerConfiguration_IsAutowired()
    {
        IEnumerable<string> exclusions = SteeltoeAssemblies.AllAssemblies.Except(new List<string>
        {
            SteeltoeAssemblies.SteeltoeExtensionsConfigurationConfigServer,
            SteeltoeAssemblies.SteeltoeExtensionsConfigurationCloudFoundry
        });

        IWebHostBuilder hostBuilder = new WebHostBuilder()
            .ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(TestHelpers.FastTestsConfiguration)).Configure(_ =>
            {
            });

        IWebHost host = hostBuilder.AddSteeltoe(exclusions).Build();
        var config = host.Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

        Assert.Equal(4, config.Providers.Count());
        Assert.Single(config.Providers.OfType<CloudFoundryConfigurationProvider>());
        Assert.Single(config.Providers.OfType<ConfigServerConfigurationProvider>());
    }

    [Fact]
    public void CloudFoundryConfiguration_IsAutowired()
    {
        IEnumerable<string> exclusions = SteeltoeAssemblies.AllAssemblies.Except(new List<string>
        {
            SteeltoeAssemblies.SteeltoeExtensionsConfigurationCloudFoundry
        });

        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddSteeltoe(exclusions).Build();
        var config = host.Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

        Assert.Equal(2, config.Providers.Count());
        Assert.Single(config.Providers.OfType<CloudFoundryConfigurationProvider>());
    }

    [Fact(Skip = "Requires Kubernetes")]
    public void KubernetesConfiguration_IsAutowired()
    {
        Environment.SetEnvironmentVariable("KUBERNETES_SERVICE_HOST", "TEST");

        IEnumerable<string> exclusions = SteeltoeAssemblies.AllAssemblies.Except(new List<string>
        {
            SteeltoeAssemblies.SteeltoeExtensionsConfigurationKubernetes
        });

        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddSteeltoe(exclusions).Build();
        var config = host.Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

        Assert.Equal(5, config.Providers.Count());
        Assert.Equal(2, config.Providers.OfType<KubernetesConfigMapProvider>().Count());
        Assert.Equal(2, config.Providers.OfType<KubernetesSecretProvider>().Count());
    }

    [Fact]
    public void RandomValueConfiguration_IsAutowired()
    {
        List<string> exclusions = SteeltoeAssemblies.AllAssemblies.Except(new List<string>
        {
            SteeltoeAssemblies.SteeltoeExtensionsConfigurationRandomValue
        }).ToList();

        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddSteeltoe(exclusions).Build();
        var config = host.Services.GetService<IConfiguration>() as ConfigurationRoot;

        Assert.Equal(2, config.Providers.Count());
        Assert.Single(config.Providers.OfType<RandomValueProvider>());
    }

    [Fact]
    public void PlaceholderResolver_IsAutowired()
    {
        IEnumerable<string> exclusions = SteeltoeAssemblies.AllAssemblies.Except(new List<string>
        {
            SteeltoeAssemblies.SteeltoeExtensionsConfigurationPlaceholder
        });

        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddSteeltoe(exclusions).Build();
        var config = host.Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

        Assert.Single(config.Providers);
        Assert.Single(config.Providers.OfType<PlaceholderResolverProvider>());
    }

    [Fact]
    public void Connectors_AreAutowired()
    {
        IEnumerable<string> exclusions = SteeltoeAssemblies.AllAssemblies.Except(new List<string>
        {
            SteeltoeAssemblies.SteeltoeConnectorConnector
        });

        IWebHostBuilder hostBuilder = new WebHostBuilder().ConfigureAppConfiguration(cfg => cfg.AddInMemoryCollection(TestHelpers.FastTestsConfiguration))
            .Configure(_ =>
            {
            });

        IWebHost host = hostBuilder.AddSteeltoe(exclusions).Build();
        var config = host.Services.GetService<IConfiguration>() as ConfigurationRoot;
        IServiceProvider services = host.Services;

        Assert.Equal(3, config.Providers.Count());
        Assert.Single(config.Providers.OfType<ConnectionStringConfigurationProvider>());
        Assert.NotNull(services.GetService<MySqlConnection>());
        Assert.NotNull(services.GetService<MongoClient>());
        Assert.NotNull(services.GetService<OracleConnection>());
        Assert.NotNull(services.GetService<NpgsqlConnection>());
        Assert.NotNull(services.GetService<ConnectionFactory>());
        Assert.NotNull(services.GetService<ConnectionMultiplexer>());
        Assert.NotNull(services.GetService<SqlConnection>());
    }

    [Fact]
    public void DynamicSerilog_IsAutowired()
    {
        IEnumerable<string> exclusions = SteeltoeAssemblies.AllAssemblies.Except(new List<string>
        {
            SteeltoeAssemblies.SteeltoeExtensionsLoggingDynamicSerilog
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
            SteeltoeAssemblies.SteeltoeManagementEndpoint
        });

        IWebHost host = new WebHostBuilder().ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(TestHelpers.WavefrontConfiguration))
            .AddSteeltoe(exclusions).Configure(_ =>
            {
            }).Build();

        var exporter = host.Services.GetService<WavefrontMetricsExporter>();

        Assert.NotNull(exporter);
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

        HttpResponseMessage response = await testClient.GetAsync("/actuator");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await testClient.GetAsync("/actuator/info");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await testClient.GetAsync("/actuator/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await testClient.GetAsync("/actuator/health/liveness");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"LivenessState\":\"CORRECT\"", await response.Content.ReadAsStringAsync());
        response = await testClient.GetAsync("/actuator/health/readiness");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"ReadinessState\":\"ACCEPTING_TRAFFIC\"", await response.Content.ReadAsStringAsync());
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
        IEnumerable<ActuatorEndpoint> managementEndpoint = host.Services.GetServices<ActuatorEndpoint>();
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
        Assert.Contains(instrumentations, obj => obj.GetType().Name.Contains("Http"));
        Assert.Contains(instrumentations, obj => obj.GetType().Name.Contains("AspNetCore"));
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
        var config = host.Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

        Assert.Equal(2, config.Providers.Count());
        Assert.Single(config.Providers.OfType<PemCertificateProvider>());
        Assert.NotNull(host.Services.GetRequiredService<IOptions<CertificateOptions>>());
        Assert.NotNull(host.Services.GetRequiredService<ICertificateRotationService>());
        Assert.NotNull(host.Services.GetRequiredService<IAuthorizationHandler>());
    }
}
