// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding.PostProcessors;
using Steeltoe.Connectors.DynamicTypeAccess;
using Steeltoe.Connectors.MongoDb.DynamicTypeAccess;

namespace Steeltoe.Connectors.MongoDb;

public static class MongoDbServiceCollectionExtensions
{
    public static IServiceCollection AddMongoDb(this IServiceCollection services, IConfigurationBuilder configurationBuilder)
    {
        return AddMongoDb(services, configurationBuilder, null);
    }

    public static IServiceCollection AddMongoDb(this IServiceCollection services, IConfigurationBuilder configurationBuilder,
        Action<ConnectorSetupOptions>? setupAction)
    {
        return AddMongoDb(services, configurationBuilder, new MongoDbPackageResolver(), setupAction);
    }

    private static IServiceCollection AddMongoDb(this IServiceCollection services, IConfigurationBuilder configurationBuilder,
        MongoDbPackageResolver packageResolver, Action<ConnectorSetupOptions>? setupAction)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configurationBuilder);
        ArgumentGuard.NotNull(packageResolver);

        var setupOptions = new ConnectorSetupOptions();
        setupAction?.Invoke(setupOptions);

        RegisterPostProcessors(configurationBuilder);

        ConnectorCreateHealthContributor? createHealthContributor = setupOptions.EnableHealthChecks
            ? (serviceProvider, serviceBindingName) => setupOptions.CreateHealthContributor != null
                ? setupOptions.CreateHealthContributor(serviceProvider, serviceBindingName)
                : CreateHealthContributor(serviceProvider, serviceBindingName, packageResolver)
            : null;

        IReadOnlySet<string> optionNames =
            ConnectorOptionsBinder.RegisterNamedOptions<MongoDbOptions>(services, configurationBuilder, "mongodb", createHealthContributor);

        ConnectorCreateConnection createConnection = (serviceProvider, serviceBindingName) => setupOptions.CreateConnection != null
            ? setupOptions.CreateConnection(serviceProvider, serviceBindingName)
            : CreateMongoClient(serviceProvider, serviceBindingName, packageResolver);

        ConnectorFactoryShim<MongoDbOptions>.Register(packageResolver.MongoClientInterface.Type, services, optionNames, createConnection, false);

        return services;
    }

    private static void RegisterPostProcessors(IConfigurationBuilder builder)
    {
        builder.AddCloudFoundryServiceBindings();
        CloudFoundryServiceBindingConfigurationSource cloudFoundrySource = builder.Sources.OfType<CloudFoundryServiceBindingConfigurationSource>().First();
        cloudFoundrySource.RegisterPostProcessor(new MongoDbCloudFoundryPostProcessor());

        var connectionStringPostProcessor = new MongoDbConnectionStringPostProcessor();
        var connectionStringSource = new ConnectionStringPostProcessorConfigurationSource();
        connectionStringSource.RegisterPostProcessor(connectionStringPostProcessor);
        builder.Add(connectionStringSource);
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string serviceBindingName,
        MongoDbPackageResolver packageResolver)
    {
        ConnectorFactoryShim<MongoDbOptions> connectorFactoryShim =
            ConnectorFactoryShim<MongoDbOptions>.FromServiceProvider(serviceProvider, packageResolver.MongoClientInterface.Type);

        ConnectorShim<MongoDbOptions> connectorShim = connectorFactoryShim.Get(serviceBindingName);

        object mongoClient = connectorShim.GetConnection();
        string hostName = GetHostNameFromConnectionString(connectorShim.Options.ConnectionString);
        var logger = serviceProvider.GetRequiredService<ILogger<MongoDbHealthContributor>>();

        return new MongoDbHealthContributor(mongoClient, $"MongoDB-{serviceBindingName}", hostName, logger);
    }

    private static string GetHostNameFromConnectionString(string? connectionString)
    {
        if (connectionString == null)
        {
            return string.Empty;
        }

        var builder = new MongoDbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        return (string)builder["server"]!;
    }

    private static object CreateMongoClient(IServiceProvider serviceProvider, string serviceBindingName, MongoDbPackageResolver packageResolver)
    {
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<MongoDbOptions>>();
        MongoDbOptions options = optionsMonitor.Get(serviceBindingName);

        var mongoClientShim = MongoClientShim.CreateInstance(packageResolver, options.ConnectionString!);
        return mongoClientShim.Instance;
    }
}
