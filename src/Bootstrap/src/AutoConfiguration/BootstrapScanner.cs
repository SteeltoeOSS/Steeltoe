// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.DynamicTypeAccess;
using Steeltoe.Common.Hosting;
using Steeltoe.Common.Logging;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Configuration.ConfigServer;
using Steeltoe.Configuration.Encryption;
using Steeltoe.Configuration.Placeholder;
using Steeltoe.Configuration.RandomValue;
using Steeltoe.Configuration.SpringBoot;
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
using Steeltoe.Logging.DynamicConsole;
using Steeltoe.Logging.DynamicSerilog;
using Steeltoe.Management.Endpoint.Actuators.All;
using Steeltoe.Management.Prometheus;
using Steeltoe.Management.Tracing;

namespace Steeltoe.Bootstrap.AutoConfiguration;

internal sealed partial class BootstrapScanner
{
    private readonly HostBuilderWrapper _wrapper;
    private readonly AssemblyLoader _loader;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private bool _isSerilogLoaded;

    public BootstrapScanner(HostBuilderWrapper wrapper, IReadOnlySet<string> assemblyNamesToExclude, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(wrapper);
        ArgumentNullException.ThrowIfNull(assemblyNamesToExclude);
        ArgumentGuard.ElementsNotNullOrWhiteSpace(assemblyNamesToExclude);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _wrapper = wrapper;
        _loader = new AssemblyLoader(assemblyNamesToExclude);
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger("Steeltoe.Bootstrap.AutoConfiguration");
    }

    public void ConfigureSteeltoe()
    {
        if (_loggerFactory is BootstrapLoggerFactory bootstrapLoggerFactory)
        {
            _wrapper.ConfigureServices(services => services.UpgradeBootstrapLoggerFactory(bootstrapLoggerFactory));
        }

        if (!WireIfLoaded(WireConfigServer, SteeltoeAssemblyNames.ConfigurationConfigServer))
        {
            WireIfLoaded(WireCloudFoundryConfiguration, SteeltoeAssemblyNames.ConfigurationCloudFoundry);
        }

        WireIfLoaded(WireRandomValueProvider, SteeltoeAssemblyNames.ConfigurationRandomValue);
        WireIfLoaded(WireSpringBootProvider, SteeltoeAssemblyNames.ConfigurationSpringBoot);
        WireIfLoaded(WireDecryptionProvider, SteeltoeAssemblyNames.ConfigurationEncryption);
        WireIfLoaded(WirePlaceholderResolver, SteeltoeAssemblyNames.ConfigurationPlaceholder);
        WireIfLoaded(WireConnectors, SteeltoeAssemblyNames.Connectors);

        _isSerilogLoaded = WireIfLoaded(WireDynamicSerilog, SteeltoeAssemblyNames.LoggingDynamicSerilog);

        if (!_isSerilogLoaded)
        {
            WireIfLoaded(WireDynamicConsole, SteeltoeAssemblyNames.LoggingDynamicConsole);
        }

        WireIfLoaded(WireDiscoveryConfiguration, SteeltoeAssemblyNames.DiscoveryConfiguration);
        WireIfLoaded(WireDiscoveryConsul, SteeltoeAssemblyNames.DiscoveryConsul);
        WireIfLoaded(WireDiscoveryEureka, SteeltoeAssemblyNames.DiscoveryEureka);
        WireIfLoaded(WireAllActuators, SteeltoeAssemblyNames.ManagementEndpoint);
        WireIfLoaded(WirePrometheus, SteeltoeAssemblyNames.ManagementPrometheus);
        WireIfLoaded(WireDistributedTracingLogProcessor, SteeltoeAssemblyNames.ManagementTracing);
    }

    private void WireConfigServer()
    {
        _wrapper.AddConfigServer(_loggerFactory);

        LogConfigServerConfigured();
    }

    private void WireCloudFoundryConfiguration()
    {
        _wrapper.AddCloudFoundryConfiguration(_loggerFactory);

        LogCloudFoundryConfigured();
    }

    private void WireRandomValueProvider()
    {
        _wrapper.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddRandomValueSource(_loggerFactory));

        LogRandomValueConfigured();
    }

    private void WireSpringBootProvider()
    {
        _wrapper.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddSpringBootFromEnvironmentVariable(_loggerFactory));

        string[] args = Environment.GetCommandLineArgs().Skip(1).ToArray();
        _wrapper.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddSpringBootFromCommandLine(args, _loggerFactory));

        LogSpringBootConfigured();
    }

    private void WireDecryptionProvider()
    {
        _wrapper.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddDecryption(_loggerFactory));

        LogDecryptionConfigured();
    }

    private void WirePlaceholderResolver()
    {
        _wrapper.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddPlaceholderResolver(_loggerFactory));

        LogPlaceholderConfigured();
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

        LogCosmosDbConfigured();
    }

    private void WireMongoDbConnector()
    {
        _wrapper.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.ConfigureMongoDb());
        _wrapper.ConfigureServices((host, services) => services.AddMongoDb(host.Configuration));

        LogMongoDbConfigured();
    }

    private void WireMySqlConnector()
    {
        _wrapper.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.ConfigureMySql());
        _wrapper.ConfigureServices((host, services) => services.AddMySql(host.Configuration));

        LogMySqlConfigured();
    }

    private void WirePostgreSqlConnector()
    {
        _wrapper.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.ConfigurePostgreSql());
        _wrapper.ConfigureServices((host, services) => services.AddPostgreSql(host.Configuration));

        LogPostgreSqlConfigured();
    }

    private void WireRabbitMQConnector()
    {
        _wrapper.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.ConfigureRabbitMQ());
        _wrapper.ConfigureServices((host, services) => services.AddRabbitMQ(host.Configuration));

        LogRabbitMQConfigured();
    }

    private void WireRedisConnector()
    {
        _wrapper.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.ConfigureRedis());
        _wrapper.ConfigureServices((host, services) => services.AddRedis(host.Configuration));

        LogRedisConfigured();

        // Intentionally ignoring excluded assemblies here.
        if (MicrosoftRedisPackageResolver.Default.IsAvailable())
        {
            LogRedisDistributedCacheConfigured();
        }
    }

    private void WireSqlServerConnector()
    {
        _wrapper.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.ConfigureSqlServer());
        _wrapper.ConfigureServices((host, services) => services.AddSqlServer(host.Configuration));

        LogSqlServerConfigured();
    }

    private void WireDynamicSerilog()
    {
        _wrapper.ConfigureLogging(loggingBuilder => loggingBuilder.AddDynamicSerilog());

        LogDynamicSerilogConfigured();
    }

    private void WireDynamicConsole()
    {
        _wrapper.ConfigureLogging(loggingBuilder => loggingBuilder.AddDynamicConsole());

        LogDynamicConsoleConfigured();
    }

    private void WireDiscoveryConfiguration()
    {
        _wrapper.ConfigureServices(services => services.AddConfigurationDiscoveryClient());

        LogConfigurationDiscoveryConfigured();
    }

    private void WireDiscoveryConsul()
    {
        _wrapper.ConfigureServices(services => services.AddConsulDiscoveryClient());

        LogConsulDiscoveryConfigured();
    }

    private void WireDiscoveryEureka()
    {
        _wrapper.ConfigureServices(services => services.AddEurekaDiscoveryClient());

        LogEurekaDiscoveryConfigured();
    }

    private void WireAllActuators()
    {
        if (_isSerilogLoaded)
        {
            _wrapper.ConfigureServices(services =>
            {
#pragma warning disable S4792 // Configuring loggers is security-sensitive
                services.AddLogging(loggingBuilder => loggingBuilder.AddDynamicSerilog());
#pragma warning restore S4792 // Configuring loggers is security-sensitive
                services.AddAllActuators();
            });
        }
        else
        {
            _wrapper.ConfigureServices(services => services.AddAllActuators());
        }

        LogActuatorsConfigured();
    }

    private void WirePrometheus()
    {
        _wrapper.ConfigureServices(services => services.AddPrometheusActuator());

        LogPrometheusConfigured();
    }

    private void WireDistributedTracingLogProcessor()
    {
        if (_isSerilogLoaded)
        {
            _wrapper.ConfigureServices(services =>
            {
#pragma warning disable S4792 // Configuring loggers is security-sensitive
                services.AddLogging(loggingBuilder => loggingBuilder.AddDynamicSerilog());
#pragma warning restore S4792 // Configuring loggers is security-sensitive
                services.AddTracingLogProcessor();
            });
        }
        else
        {
            _wrapper.ConfigureServices(services => services.AddTracingLogProcessor());
        }

        LogDistributedTracingConfigured();
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Configured Config Server configuration provider.")]
    private partial void LogConfigServerConfigured();

    [LoggerMessage(Level = LogLevel.Information, Message = "Configured Cloud Foundry configuration provider.")]
    private partial void LogCloudFoundryConfigured();

    [LoggerMessage(Level = LogLevel.Information, Message = "Configured random value configuration provider.")]
    private partial void LogRandomValueConfigured();

    [LoggerMessage(Level = LogLevel.Information, Message = "Configured Spring Boot configuration provider.")]
    private partial void LogSpringBootConfigured();

    [LoggerMessage(Level = LogLevel.Information, Message = "Configured decryption configuration provider.")]
    private partial void LogDecryptionConfigured();

    [LoggerMessage(Level = LogLevel.Information, Message = "Configured placeholder configuration provider.")]
    private partial void LogPlaceholderConfigured();

    [LoggerMessage(Level = LogLevel.Information, Message = "Configured CosmosDB connector.")]
    private partial void LogCosmosDbConfigured();

    [LoggerMessage(Level = LogLevel.Information, Message = "Configured MongoDB connector.")]
    private partial void LogMongoDbConfigured();

    [LoggerMessage(Level = LogLevel.Information, Message = "Configured MySQL connector.")]
    private partial void LogMySqlConfigured();

    [LoggerMessage(Level = LogLevel.Information, Message = "Configured PostgreSQL connector.")]
    private partial void LogPostgreSqlConfigured();

    [LoggerMessage(Level = LogLevel.Information, Message = "Configured RabbitMQ connector.")]
    private partial void LogRabbitMQConfigured();

    [LoggerMessage(Level = LogLevel.Information, Message = "Configured StackExchange Redis connector.")]
    private partial void LogRedisConfigured();

    [LoggerMessage(Level = LogLevel.Information, Message = "Configured Redis distributed cache connector.")]
    private partial void LogRedisDistributedCacheConfigured();

    [LoggerMessage(Level = LogLevel.Information, Message = "Configured SQL Server connector.")]
    private partial void LogSqlServerConfigured();

    [LoggerMessage(Level = LogLevel.Information, Message = "Configured dynamic console logger for Serilog.")]
    private partial void LogDynamicSerilogConfigured();

    [LoggerMessage(Level = LogLevel.Information, Message = "Configured dynamic console logger.")]
    private partial void LogDynamicConsoleConfigured();

    [LoggerMessage(Level = LogLevel.Information, Message = "Configured configuration discovery client.")]
    private partial void LogConfigurationDiscoveryConfigured();

    [LoggerMessage(Level = LogLevel.Information, Message = "Configured Consul discovery client.")]
    private partial void LogConsulDiscoveryConfigured();

    [LoggerMessage(Level = LogLevel.Information, Message = "Configured Eureka discovery client.")]
    private partial void LogEurekaDiscoveryConfigured();

    [LoggerMessage(Level = LogLevel.Information, Message = "Configured actuators.")]
    private partial void LogActuatorsConfigured();

    [LoggerMessage(Level = LogLevel.Information, Message = "Configured Prometheus.")]
    private partial void LogPrometheusConfigured();

    [LoggerMessage(Level = LogLevel.Information, Message = "Configured distributed tracing log processor.")]
    private partial void LogDistributedTracingConfigured();
}
