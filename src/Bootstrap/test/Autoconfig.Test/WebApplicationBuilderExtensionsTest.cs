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
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.OpenTelemetry.Exporters;
using Steeltoe.Management.OpenTelemetry.Trace;
using Xunit;

namespace Steeltoe.Bootstrap.Autoconfig.Test;

public class WebApplicationBuilderExtensionsTest
{
    [Fact]
    public void ConfigServerConfiguration_IsAutowired()
    {
        WebApplication host = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.SteeltoeExtensionsConfigurationConfigServer,
            SteeltoeAssemblies.SteeltoeExtensionsConfigurationCloudFoundry);

        var config = host.Services.GetServices<IConfiguration>().First(c => c is ConfigurationManager) as IConfigurationRoot;

        // WebApplication.CreateBuilder() automatically includes a few builders
        Assert.Equal(9, config.Providers.Count());
        Assert.Single(config.Providers.OfType<CloudFoundryConfigurationProvider>());
        Assert.Single(config.Providers.OfType<ConfigServerConfigurationProvider>());
    }

    [Fact]
    public void CloudFoundryConfiguration_IsAutowired()
    {
        WebApplication host = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.SteeltoeExtensionsConfigurationCloudFoundry);
        var config = host.Services.GetServices<IConfiguration>().First(c => c is ConfigurationManager) as IConfigurationRoot;

        Assert.Equal(8, config.Providers.Count());
        Assert.Single(config.Providers.OfType<CloudFoundryConfigurationProvider>());
    }

    [Fact(Skip = "Requires Kubernetes")]
    public void KubernetesConfiguration_IsAutowired()
    {
        Environment.SetEnvironmentVariable("KUBERNETES_SERVICE_HOST", "TEST");
        WebApplication host = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.SteeltoeExtensionsConfigurationKubernetes);
        var config = host.Services.GetServices<IConfiguration>().First(c => c is ConfigurationManager) as IConfigurationRoot;

        Assert.Equal(11, config.Providers.Count());
        Assert.Equal(2, config.Providers.OfType<KubernetesConfigMapProvider>().Count());
        Assert.Equal(2, config.Providers.OfType<KubernetesSecretProvider>().Count());
    }

    [Fact]
    public void RandomValueConfiguration_IsAutowired()
    {
        WebApplication host = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.SteeltoeExtensionsConfigurationRandomValue);
        var config = host.Services.GetServices<IConfiguration>().First(c => c is ConfigurationManager) as IConfigurationRoot;

        Assert.Equal(8, config.Providers.Count());
        Assert.Single(config.Providers.OfType<RandomValueProvider>());
    }

    [Fact]
    public void PlaceholderResolver_IsAutowired()
    {
        WebApplication host = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.SteeltoeExtensionsConfigurationPlaceholder);
        var config = host.Services.GetServices<IConfiguration>().First(c => c is ConfigurationManager) as IConfigurationRoot;
        Assert.Single(config.Providers.OfType<PlaceholderResolverProvider>());
    }

    [Fact]
    public void Connectors_AreAutowired()
    {
        WebApplication host = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.SteeltoeConnectorConnector);
        var config = host.Services.GetServices<IConfiguration>().First(c => c is ConfigurationManager) as IConfigurationRoot;
        IServiceProvider services = host.Services;

        Assert.Equal(8, config.Providers.Count());
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
        WebApplication host = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.SteeltoeExtensionsLoggingDynamicSerilog);

        var loggerProvider = (IDynamicLoggerProvider)host.Services.GetService(typeof(IDynamicLoggerProvider));

        Assert.IsType<SerilogDynamicProvider>(loggerProvider);
    }

    [Fact]
    public void ServiceDiscovery_IsAutowired()
    {
        WebApplication host = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.SteeltoeDiscoveryClient);
        IEnumerable<IDiscoveryClient> discoveryClient = host.Services.GetServices<IDiscoveryClient>();

        Assert.Single(discoveryClient);
        Assert.IsType<EurekaDiscoveryClient>(discoveryClient.First());
    }

    [Fact]
    public async Task KubernetesActuators_AreAutowired()
    {
        WebApplication webApp = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.SteeltoeManagementKubernetes);
        webApp.UseRouting();
        await webApp.StartAsync();

        IEnumerable<ActuatorEndpoint> managementEndpoint = webApp.Services.GetServices<ActuatorEndpoint>();
        IStartupFilter filter = webApp.Services.GetServices<IStartupFilter>().FirstOrDefault();
        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);

        await ActuatorTestAsync(webApp.GetTestClient());
    }

    [Fact]
    public async Task AllActuators_AreAutowired()
    {
        WebApplication webApp = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.SteeltoeManagementEndpoint);
        webApp.UseRouting();
        await webApp.StartAsync();

        IEnumerable<ActuatorEndpoint> managementEndpoint = webApp.Services.GetServices<ActuatorEndpoint>();
        IStartupFilter filter = webApp.Services.GetServices<IStartupFilter>().FirstOrDefault(f => f is AllActuatorsStartupFilter);

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);

        await ActuatorTestAsync(webApp.GetTestClient());
    }

    [Fact]
    public async Task WavefrontMetricsExporter_IsAutowired()
    {
        WebApplicationBuilder webAppBuilder = WebApplication.CreateBuilder();
        webAppBuilder.Configuration.AddInMemoryCollection(TestHelpers.WavefrontConfiguration);

        var exclusions = new List<string>
        {
            SteeltoeAssemblies.SteeltoeManagementEndpoint
        };

        webAppBuilder.AddSteeltoe(SteeltoeAssemblies.AllAssemblies.Except(exclusions));
        webAppBuilder.WebHost.UseTestServer();
        WebApplication webApp = webAppBuilder.Build();

        webApp.UseRouting();
        await webApp.StartAsync();
        var exporter = webApp.Services.GetService<WavefrontMetricsExporter>();

        Assert.NotNull(exporter);
    }

    [Fact]
    public async Task WavefrontTraceExporter_IsAutowired()
    {
        WebApplicationBuilder webAppBuilder = WebApplication.CreateBuilder();
        webAppBuilder.Configuration.AddInMemoryCollection(TestHelpers.WavefrontConfiguration);

        var exclusions = new List<string>
        {
            SteeltoeAssemblies.SteeltoeManagementTracing
        };

        webAppBuilder.AddSteeltoe(SteeltoeAssemblies.AllAssemblies.Except(exclusions));
        webAppBuilder.WebHost.UseTestServer();
        WebApplication webApp = webAppBuilder.Build();

        webApp.UseRouting();
        await webApp.StartAsync();

        var tracerProvider = webApp.Services.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);

        object processor = tracerProvider.GetType().GetProperty("Processor", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(tracerProvider);

        object exporter = processor.GetType().GetField("exporter", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(processor);
        Assert.NotNull(exporter);
        Assert.IsType<WavefrontTraceExporter>(exporter);
    }

    [Fact]
    public void Tracing_IsAutowired()
    {
        WebApplication host = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.SteeltoeManagementTracing);
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
        WebApplication host = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.SteeltoeSecurityAuthenticationCloudFoundry);
        var config = host.Services.GetServices<IConfiguration>().First(c => c is ConfigurationManager) as IConfigurationRoot;

        Assert.Equal(8, config.Providers.Count());
        Assert.Single(config.Providers.OfType<PemCertificateProvider>());
        Assert.NotNull(host.Services.GetRequiredService<IOptions<CertificateOptions>>());
        Assert.NotNull(host.Services.GetRequiredService<ICertificateRotationService>());
        Assert.NotNull(host.Services.GetRequiredService<IAuthorizationHandler>());
    }

    private WebApplication GetWebApplicationWithSteeltoe(params string[] steeltoeInclusions)
    {
        WebApplicationBuilder webAppBuilder = WebApplication.CreateBuilder();
        webAppBuilder.Configuration.AddInMemoryCollection(TestHelpers.FastTestsConfiguration);
        webAppBuilder.AddSteeltoe(SteeltoeAssemblies.AllAssemblies.Except(steeltoeInclusions));
        webAppBuilder.WebHost.UseTestServer();
        return webAppBuilder.Build();
    }

    private async Task ActuatorTestAsync(HttpClient testClient)
    {
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
}
