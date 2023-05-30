// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding.PostProcessors;
using Steeltoe.Configuration.Kubernetes.ServiceBinding;
using Steeltoe.Configuration.Kubernetes.ServiceBinding.PostProcessors;
using Steeltoe.Connectors.MySql.RuntimeTypeAccess;
using Steeltoe.Connectors.RuntimeTypeAccess;

namespace Steeltoe.Connectors.MySql;

public static class MySqlServiceCollectionExtensions
{
    public static IServiceCollection AddMySql(this IServiceCollection services, IConfigurationBuilder configurationBuilder)
    {
        return AddMySql(services, configurationBuilder, null);
    }

    public static IServiceCollection AddMySql(this IServiceCollection services, IConfigurationBuilder configurationBuilder,
        Action<ConnectorSetupOptions>? setupAction)
    {
        return AddMySql(services, configurationBuilder, new MySqlPackageResolver(), setupAction);
    }

    internal static IServiceCollection AddMySql(this IServiceCollection services, IConfigurationBuilder configurationBuilder,
        MySqlPackageResolver packageResolver, Action<ConnectorSetupOptions>? setupAction)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configurationBuilder);
        ArgumentGuard.NotNull(packageResolver);

        var setupOptions = new ConnectorSetupOptions();
        setupAction?.Invoke(setupOptions);

        RegisterPostProcessors(configurationBuilder, packageResolver);

        ConnectorCreateHealthContributor? createHealthContributor = setupOptions.EnableHealthChecks
            ? (serviceProvider, serviceBindingName) => setupOptions.CreateHealthContributor != null
                ? setupOptions.CreateHealthContributor(serviceProvider, serviceBindingName)
                : CreateHealthContributor(serviceProvider, serviceBindingName, packageResolver)
            : null;

        IReadOnlySet<string> optionNames =
            ConnectorOptionsBinder.RegisterNamedOptions<MySqlOptions>(services, configurationBuilder, "mysql", createHealthContributor);

        ConnectorCreateConnection createConnection = (serviceProvider, serviceBindingName) => setupOptions.CreateConnection != null
            ? setupOptions.CreateConnection(serviceProvider, serviceBindingName)
            : CreateConnection(serviceProvider, serviceBindingName, packageResolver);

        ConnectorFactoryShim<MySqlOptions>.Register(packageResolver.MySqlConnectionClass.Type, services, optionNames, createConnection, false);

        return services;
    }

    private static void RegisterPostProcessors(IConfigurationBuilder builder, MySqlPackageResolver packageResolver)
    {
        builder.AddCloudFoundryServiceBindings();
        CloudFoundryServiceBindingConfigurationSource cloudFoundrySource = builder.Sources.OfType<CloudFoundryServiceBindingConfigurationSource>().First();
        cloudFoundrySource.RegisterPostProcessor(new MySqlCloudFoundryPostProcessor());

        builder.AddKubernetesServiceBindings();
        KubernetesServiceBindingConfigurationSource kubernetesSource = builder.Sources.OfType<KubernetesServiceBindingConfigurationSource>().First();
        kubernetesSource.RegisterPostProcessor(new MySqlKubernetesPostProcessor());

        var connectionStringPostProcessor = new MySqlConnectionStringPostProcessor(packageResolver);
        var connectionStringSource = new ConnectionStringPostProcessorConfigurationSource();
        connectionStringSource.RegisterPostProcessor(connectionStringPostProcessor);
        builder.Add(connectionStringSource);
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string serviceBindingName, MySqlPackageResolver packageResolver)
    {
        ConnectorFactoryShim<MySqlOptions> connectorFactoryShim =
            ConnectorFactoryShim<MySqlOptions>.FromServiceProvider(serviceProvider, packageResolver.MySqlConnectionClass.Type);

        ConnectorShim<MySqlOptions> connectorShim = connectorFactoryShim.GetNamed(serviceBindingName);

        var connection = (DbConnection)connectorShim.GetConnection();
        string hostName = GetHostNameFromConnectionString(packageResolver, connectorShim.Options.ConnectionString);
        var logger = serviceProvider.GetRequiredService<ILogger<RelationalDbHealthContributor>>();

        return new RelationalDbHealthContributor(connection, $"MySQL-{serviceBindingName}", hostName, logger);
    }

    private static string GetHostNameFromConnectionString(MySqlPackageResolver packageResolver, string? connectionString)
    {
        var connectionStringBuilderShim = MySqlConnectionStringBuilderShim.CreateInstance(packageResolver);
        connectionStringBuilderShim.Instance.ConnectionString = connectionString;
        return (string)connectionStringBuilderShim.Instance["host"];
    }

    private static DbConnection CreateConnection(IServiceProvider serviceProvider, string serviceBindingName, MySqlPackageResolver packageResolver)
    {
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<MySqlOptions>>();
        MySqlOptions options = optionsMonitor.Get(serviceBindingName);

        var mySqlConnectionShim = MySqlConnectionShim.CreateInstance(packageResolver, options.ConnectionString);
        return mySqlConnectionShim.Instance;
    }
}
