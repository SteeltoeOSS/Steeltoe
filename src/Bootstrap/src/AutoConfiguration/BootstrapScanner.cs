// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.DynamicTypeAccess;
using Steeltoe.Common.Hosting;
using Steeltoe.Common.Logging;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Configuration.ConfigServer;
using Steeltoe.Configuration.Placeholder;
using Steeltoe.Configuration.RandomValue;
using Steeltoe.Connectors.CosmosDb;
using Steeltoe.Connectors.CosmosDb.DynamicTypeAccess;
using Steeltoe.Connectors.MongoDb;
using Steeltoe.Connectors.MongoDb.DynamicTypeAccess;
using Steeltoe.Connectors.MySql;
using Steeltoe.Connectors.MySql.DynamicTypeAccess;
using Steeltoe.Connectors.PostgreSql;
using Steeltoe.Connectors.PostgreSql.DynamicTypeAccess;
using Steeltoe.Connectors.RabbitMQ;
using Steeltoe.Connectors.RabbitMQ.DynamicTypeAccess;
using Steeltoe.Connectors.Redis;
using Steeltoe.Connectors.Redis.DynamicTypeAccess;
using Steeltoe.Connectors.SqlServer;
using Steeltoe.Connectors.SqlServer.RuntimeTypeAccess;
using Steeltoe.Discovery.Configuration;
using Steeltoe.Discovery.Consul;
using Steeltoe.Discovery.Eureka;
using Steeltoe.Logging.DynamicSerilog;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Prometheus;
using Steeltoe.Management.Tracing;
using Steeltoe.Management.Wavefront;
using Steeltoe.Management.Wavefront.Exporters;
using Steeltoe.Security.Authentication.CloudFoundry;

namespace Steeltoe.Bootstrap.AutoConfiguration;

internal sealed class BootstrapScanner
{
    private readonly HostBuilderWrapper _wrapper;
    private readonly AssemblyLoader _loader;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;

    public BootstrapScanner(HostBuilderWrapper wrapper, IReadOnlySet<string> assemblyNamesToExclude, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(wrapper);
        ArgumentGuard.NotNull(assemblyNamesToExclude);
        ArgumentGuard.NotNull(loggerFactory);

        _wrapper = wrapper;
        _loader = new AssemblyLoader(assemblyNamesToExclude);
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger("Steeltoe.Bootstrap.AutoConfiguration");
    }

    public void ConfigureSteeltoe()
    {
        if (_loggerFactory is IBootstrapLoggerFactory)
        {
            BootstrapLoggerHostedService.Register(_loggerFactory, _wrapper);
        }

        if (!WireIfLoaded(WireConfigServer, SteeltoeAssemblyNames.ConfigurationConfigServer))
        {
            WireIfLoaded(WireCloudFoundryConfiguration, SteeltoeAssemblyNames.ConfigurationCloudFoundry);
        }

        WireIfLoaded(WireRandomValueProvider, SteeltoeAssemblyNames.ConfigurationRandomValue);
        WireIfLoaded(WirePlaceholderResolver, SteeltoeAssemblyNames.ConfigurationPlaceholder);
        WireIfLoaded(WireConnectors, SteeltoeAssemblyNames.Connectors);
        WireIfLoaded(WireDynamicSerilog, SteeltoeAssemblyNames.LoggingDynamicSerilog);
        WireIfLoaded(WireDiscoveryConfiguration, SteeltoeAssemblyNames.DiscoveryConfiguration);
        WireIfLoaded(WireDiscoveryConsul, SteeltoeAssemblyNames.DiscoveryConsul);
        WireIfLoaded(WireDiscoveryEureka, SteeltoeAssemblyNames.DiscoveryEureka);
        WireIfLoaded(WireAllActuators, SteeltoeAssemblyNames.ManagementEndpoint);
        WireIfLoaded(WirePrometheus, SteeltoeAssemblyNames.ManagementPrometheus);
        WireIfLoaded(WireWavefrontMetrics, SteeltoeAssemblyNames.ManagementWavefront);
        WireIfLoaded(WireDistributedTracing, SteeltoeAssemblyNames.ManagementTracing);
        WireIfLoaded(WireCloudFoundryContainerIdentity, SteeltoeAssemblyNames.SecurityAuthenticationCloudFoundry);
    }

    private void WireConfigServer()
    {
        _wrapper.AddConfigServer(_loggerFactory);

        _logger.LogInformation("Configured Config Server configuration provider");
    }

    private void WireCloudFoundryConfiguration()
    {
        _wrapper.AddCloudFoundryConfiguration(_loggerFactory);

        _logger.LogInformation("Configured Cloud Foundry configuration provider");
    }

    private void WireRandomValueProvider()
    {
        _wrapper.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddRandomValueSource(_loggerFactory));

        _logger.LogInformation("Configured random value configuration provider");
    }

    private void WirePlaceholderResolver()
    {
        _wrapper.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddPlaceholderResolver(_loggerFactory));

        _logger.LogInformation("Configured placeholder configuration provider");
    }

    private void WireConnectors()
    {
        WireIfAnyLoaded(WireCosmosDbConnector, CosmosDbPackageResolver.Default);
        WireIfAnyLoaded(WireMongoDbConnector, MongoDbPackageResolver.Default);
        WireIfAnyLoaded(WireMySqlConnector, MySqlPackageResolver.Default);
        WireIfAnyLoaded(WirePostgreSqlConnector, PostgreSqlPackageResolver.Default);
        WireIfAnyLoaded(WireRabbitMQConnector, RabbitMQPackageResolver.Default);
        WireIfAnyLoaded(WireRedisConnector, StackExchangeRedisPackageResolver.Default, MicrosoftRedisPackageResolver.Default);
        WireIfAnyLoaded(WireSqlServerConnector, SqlServerPackageResolver.Default);
    }

    private void WireCosmosDbConnector()
    {
        _wrapper.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.ConfigureCosmosDb());
        _wrapper.ConfigureServices((host, services) => services.AddCosmosDb(host.Configuration));

        _logger.LogInformation("Configured CosmosDB connector");
    }

    private void WireMongoDbConnector()
    {
        _wrapper.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.ConfigureMongoDb());
        _wrapper.ConfigureServices((host, services) => services.AddMongoDb(host.Configuration));

        _logger.LogInformation("Configured MongoDB connector");
    }

    private void WireMySqlConnector()
    {
        _wrapper.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.ConfigureMySql());
        _wrapper.ConfigureServices((host, services) => services.AddMySql(host.Configuration));

        _logger.LogInformation("Configured MySQL connector");
    }

    private void WirePostgreSqlConnector()
    {
        _wrapper.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.ConfigurePostgreSql());
        _wrapper.ConfigureServices((host, services) => services.AddPostgreSql(host.Configuration));

        _logger.LogInformation("Configured PostgreSQL connector");
    }

    private void WireRabbitMQConnector()
    {
        _wrapper.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.ConfigureRabbitMQ());
        _wrapper.ConfigureServices((host, services) => services.AddRabbitMQ(host.Configuration));

        _logger.LogInformation("Configured RabbitMQ connector");
    }

    private void WireRedisConnector()
    {
        _wrapper.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.ConfigureRedis());
        _wrapper.ConfigureServices((host, services) => services.AddRedis(host.Configuration));

        _logger.LogInformation("Configured StackExchange Redis connector");

        // Intentionally ignoring excluded assemblies here.
        if (MicrosoftRedisPackageResolver.Default.IsAvailable())
        {
            _logger.LogInformation("Configured Redis distributed cache connector");
        }
    }

    private void WireSqlServerConnector()
    {
        _wrapper.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.ConfigureSqlServer());
        _wrapper.ConfigureServices((host, services) => services.AddSqlServer(host.Configuration));

        _logger.LogInformation("Configured SQL Server connector");
    }

    private void WireDynamicSerilog()
    {
        _wrapper.AddDynamicSerilog(null, false);

        _logger.LogInformation("Configured dynamic console logger for Serilog");
    }

    private void WireDiscoveryConfiguration()
    {
        _wrapper.ConfigureServices(services => services.AddConfigurationDiscoveryClient());

        _logger.LogInformation("Configured configuration discovery client");
    }

    private void WireDiscoveryConsul()
    {
        _wrapper.ConfigureServices(services => services.AddConsulDiscoveryClient());

        _logger.LogInformation("Configured Consul discovery client");
    }

    private void WireDiscoveryEureka()
    {
        _wrapper.ConfigureServices(services => services.AddEurekaDiscoveryClient());

        _logger.LogInformation("Configured Eureka discovery client");
    }

    private void WireAllActuators()
    {
        _wrapper.AddAllActuators(null, MediaTypeVersion.V2, null);

        _logger.LogInformation("Configured actuators");
    }

    private void WirePrometheus()
    {
        _wrapper.ConfigureServices(services => services.AddPrometheusActuator());

        _logger.LogInformation("Configured Prometheus");
    }

    private void WireWavefrontMetrics()
    {
        _wrapper.ConfigureServices((context, services) =>
        {
            if (HasWavefront(context.Configuration))
            {
                services.AddWavefrontMetrics();
                _logger.LogInformation("Configured Wavefront metrics");
            }
        });
    }

    private static bool HasWavefront(IConfiguration configuration)
    {
        var options = new WavefrontExporterOptions();

        var configurer = new ConfigureWavefrontExporterOptions(configuration);
        configurer.Configure(options);

        return !string.IsNullOrEmpty(options.Uri);
    }

    private void WireDistributedTracing()
    {
        _wrapper.ConfigureServices(services => services.AddDistributedTracingAspNetCore());

        _logger.LogInformation("Configured distributed tracing");
    }

    private void WireCloudFoundryContainerIdentity()
    {
        _wrapper.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddCloudFoundryContainerIdentity());
        _wrapper.ConfigureServices(services => services.AddCloudFoundryCertificateAuth());

        _logger.LogInformation("Configured Cloud Foundry mTLS security");
    }

    private bool WireIfLoaded(Action wireAction, string assemblyName)
    {
        if (!_loader.IsAssemblyLoaded(assemblyName))
        {
            return false;
        }

        wireAction();
        return true;
    }

    private void WireIfAnyLoaded(Action wireAction, params PackageResolver[] packageResolvers)
    {
        if (Array.Exists(packageResolvers, packageResolver => packageResolver.IsAvailable(_loader.AssemblyNamesToExclude)))
        {
            wireAction();
        }
    }
}
