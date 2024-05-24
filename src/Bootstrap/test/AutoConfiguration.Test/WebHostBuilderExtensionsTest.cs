// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore;
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
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Options;
using Steeltoe.Common.Security;
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
using Steeltoe.Management.Endpoint.Web.Hypermedia;
using Steeltoe.Management.Wavefront.Exporters;
using Xunit;

namespace Steeltoe.Bootstrap.AutoConfiguration.Test;

public sealed class WebHostBuilderExtensionsTest
{
    [Fact]
    public void ConfigServerConfiguration_IsAutowired()
    {
        using IWebHost host = GetWebHostForOnly(SteeltoeAssemblyNames.ConfigurationConfigServer);
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        configuration.FindConfigurationProvider<CloudFoundryConfigurationProvider>().Should().NotBeNull();
        configuration.FindConfigurationProvider<ConfigServerConfigurationProvider>().Should().NotBeNull();
    }

    [Fact]
    public void CloudFoundryConfiguration_IsAutowired()
    {
        using IWebHost host = GetWebHostForOnly(SteeltoeAssemblyNames.ConfigurationCloudFoundry);
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        configuration.FindConfigurationProvider<CloudFoundryConfigurationProvider>().Should().NotBeNull();
    }

    [Fact]
    public void RandomValueConfiguration_IsAutowired()
    {
        using IWebHost host = GetWebHostForOnly(SteeltoeAssemblyNames.ConfigurationRandomValue);
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        configuration.FindConfigurationProvider<RandomValueProvider>().Should().NotBeNull();
    }

    [Fact]
    public void PlaceholderResolver_IsAutowired()
    {
        using IWebHost host = GetWebHostForOnly(SteeltoeAssemblyNames.ConfigurationPlaceholder);
        var configurationRoot = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();

        configurationRoot.Providers.OfType<PlaceholderResolverProvider>().Should().HaveCount(1);
    }

    [Fact]
    public void Connectors_AreAutowired()
    {
        using IWebHost host = GetWebHostForOnly(SteeltoeAssemblyNames.Connectors);
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        configuration.FindConfigurationProvider<KubernetesServiceBindingConfigurationProvider>().Should().NotBeNull();
        configuration.FindConfigurationProvider<CloudFoundryServiceBindingConfigurationProvider>().Should().NotBeNull();

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
    public void SqlServerConnector_NotAutowiredIfDependenciesExcluded()
    {
        var exclusions = new HashSet<string>(SteeltoeAssemblyNames.All);
        exclusions.Remove(SteeltoeAssemblyNames.Connectors);
        exclusions.Add("Microsoft.Data.SqlClient");
        exclusions.Add("System.Data.SqlClient");

        using IWebHost host = GetWebHostExcluding(exclusions);

        host.Services.GetService<ConnectorFactory<SqlServerOptions, SqlConnection>>().Should().BeNull();
    }

    [Fact]
    public void DynamicSerilog_IsAutowired()
    {
        using IWebHost host = GetWebHostForOnly(SteeltoeAssemblyNames.LoggingDynamicSerilog);

        var loggerProvider = host.Services.GetRequiredService<IDynamicLoggerProvider>();

        loggerProvider.Should().BeOfType<DynamicSerilogLoggerProvider>();
    }

    [Fact]
    public void ServiceDiscoveryClients_AreAutowired()
    {
        var assembliesToInclude = new HashSet<string>
        {
            SteeltoeAssemblyNames.DiscoveryConfiguration,
            SteeltoeAssemblyNames.DiscoveryConsul,
            SteeltoeAssemblyNames.DiscoveryEureka
        };

        using IWebHost host = GetWebHostExcluding(SteeltoeAssemblyNames.All.Except(assembliesToInclude).ToHashSet());

        IDiscoveryClient[] discoveryClients = host.Services.GetServices<IDiscoveryClient>().ToArray();

        discoveryClients.Should().HaveCount(3);
        discoveryClients.Should().ContainSingle(discoveryClient => discoveryClient is ConfigurationDiscoveryClient);
        discoveryClients.Should().ContainSingle(discoveryClient => discoveryClient is ConsulDiscoveryClient);
        discoveryClients.Should().ContainSingle(discoveryClient => discoveryClient is EurekaDiscoveryClient);
    }

    [Fact]
    public void Prometheus_IsAutowired()
    {
        using IWebHost host = GetWebHostForOnly(SteeltoeAssemblyNames.ManagementPrometheus);

        host.Services.GetService<MeterProvider>().Should().NotBeNull();
    }

    [Fact]
    public void WavefrontMetricsExporter_IsAutowired()
    {
        using IWebHost host = GetWebHostForOnly(SteeltoeAssemblyNames.ManagementWavefront);

        host.Services.GetService<MeterProvider>().Should().NotBeNull();
    }

    [Fact]
    public void WavefrontTraceExporter_IsAutowired()
    {
        using IWebHost host = GetWebHostForOnly(SteeltoeAssemblyNames.ManagementTracing);

        var tracerProvider = host.Services.GetRequiredService<TracerProvider>();

        PropertyInfo? processorProperty = tracerProvider.GetType().GetProperty("Processor", BindingFlags.NonPublic | BindingFlags.Instance);
        processorProperty.Should().NotBeNull();

        object? processor = processorProperty!.GetValue(tracerProvider);
        processor.Should().NotBeNull();

        FieldInfo? exporterField = processor!.GetType().GetField("exporter", BindingFlags.NonPublic | BindingFlags.Instance);
        exporterField.Should().NotBeNull();

        object? exporter = exporterField!.GetValue(processor);
        exporter.Should().BeOfType<WavefrontTraceExporter>();
    }

    [Fact]
    public async Task AllActuators_AreAutowired()
    {
        using IWebHost host = GetWebHostForOnly(SteeltoeAssemblyNames.ManagementEndpoint);
        await host.StartAsync();

        IActuatorEndpointHandler[] handlers = host.Services.GetServices<IActuatorEndpointHandler>().ToArray();
        handlers.Should().HaveCount(1);

        var filter = host.Services.GetRequiredService<IStartupFilter>();
        filter.Should().BeOfType<AllActuatorsStartupFilter>();

        using HttpClient testClient = host.GetTestClient();
        await WebApplicationBuilderExtensionsTest.AssertActuatorEndpointsSucceedAsync(testClient);
    }

    [Fact]
    public void Tracing_IsAutowired()
    {
        using IWebHost host = GetWebHostForOnly(SteeltoeAssemblyNames.ManagementTracing);
        var tracerProvider = host.Services.GetRequiredService<TracerProvider>();

        host.Services.GetService<IHostedService>().Should().NotBeNull();
        host.Services.GetService<ITracingOptions>().Should().NotBeNull();
        host.Services.GetService<IDynamicMessageProcessor>().Should().NotBeNull();

        FieldInfo? instrumentationsField = tracerProvider.GetType().GetField("instrumentations", BindingFlags.NonPublic | BindingFlags.Instance);
        instrumentationsField.Should().NotBeNull();

        var instrumentations = (List<object>?)instrumentationsField!.GetValue(tracerProvider);

        instrumentations.Should().HaveCount(2);
        instrumentations.Should().ContainSingle(instance => instance.GetType().Name == "HttpClientInstrumentation");
        instrumentations.Should().ContainSingle(instance => instance.GetType().Name == "AspNetCoreInstrumentation");
    }

    [Fact]
    public void CloudFoundryContainerSecurity_IsAutowired()
    {
        using IWebHost host = GetWebHostForOnly(SteeltoeAssemblyNames.SecurityAuthenticationCloudFoundry);
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        configuration.FindConfigurationProvider<PemCertificateProvider>().Should().NotBeNull();

        host.Services.GetService<IOptions<CertificateOptions>>().Should().NotBeNull();
        host.Services.GetService<CertificateRotationService>().Should().NotBeNull();
        host.Services.GetService<IAuthorizationHandler>().Should().NotBeNull();
    }

    private static IWebHost GetWebHostForOnly(string assemblyNameToInclude)
    {
        IReadOnlySet<string> exclusions = SteeltoeAssemblyNames.Only(assemblyNameToInclude);
        return GetWebHostExcluding(exclusions);
    }

    private static IWebHost GetWebHostExcluding(IReadOnlySet<string> assemblyNamesToExclude)
    {
        IWebHostBuilder builder = WebHost.CreateDefaultBuilder();
        builder.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(TestHelpers.FastTestsConfiguration));
        builder.ConfigureServices(services => services.AddActionDescriptorCollectionProvider());
        builder.Configure(applicationBuilder => applicationBuilder.UseRouting());
        builder.UseTestServer();

        builder.AddSteeltoe(assemblyNamesToExclude);

        return builder.Build();
    }
}
