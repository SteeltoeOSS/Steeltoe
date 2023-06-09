// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.DynamicTypeAccess;
using Steeltoe.Connectors.MySql.DynamicTypeAccess;

namespace Steeltoe.Connectors.MySql;

public static class MySqlServiceCollectionExtensions
{
    public static IServiceCollection AddMySql(this IServiceCollection services, IConfiguration configuration)
    {
        return AddMySql(services, configuration, null);
    }

    public static IServiceCollection AddMySql(this IServiceCollection services, IConfiguration configuration, Action<ConnectorAddOptions>? addAction)
    {
        return AddMySql(services, configuration, MySqlPackageResolver.Default, addAction);
    }

    internal static IServiceCollection AddMySql(this IServiceCollection services, IConfiguration configuration, MySqlPackageResolver packageResolver,
        Action<ConnectorAddOptions>? addAction)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(packageResolver);

        if (!ConnectorFactoryShim<MySqlOptions>.IsRegistered(packageResolver.MySqlConnectionClass.Type, services))
        {
            var addOptions = new ConnectorAddOptions(
                (serviceProvider, serviceBindingName) => CreateConnection(serviceProvider, serviceBindingName, packageResolver),
                (serviceProvider, serviceBindingName) => CreateHealthContributor(serviceProvider, serviceBindingName, packageResolver))
            {
                CacheConnection = false,
                EnableHealthChecks = services.All(descriptor => descriptor.ServiceType != typeof(HealthCheckService))
            };

            addAction?.Invoke(addOptions);

            IReadOnlySet<string> optionNames = ConnectorOptionsBinder.RegisterNamedOptions<MySqlOptions>(services, configuration, "mysql",
                addOptions.EnableHealthChecks ? addOptions.CreateHealthContributor : null);

            ConnectorFactoryShim<MySqlOptions>.Register(packageResolver.MySqlConnectionClass.Type, services, optionNames, addOptions.CreateConnection,
                addOptions.CacheConnection);
        }

        return services;
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string serviceBindingName, MySqlPackageResolver packageResolver)
    {
        ConnectorFactoryShim<MySqlOptions> connectorFactoryShim =
            ConnectorFactoryShim<MySqlOptions>.FromServiceProvider(serviceProvider, packageResolver.MySqlConnectionClass.Type);

        ConnectorShim<MySqlOptions> connectorShim = connectorFactoryShim.Get(serviceBindingName);

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
