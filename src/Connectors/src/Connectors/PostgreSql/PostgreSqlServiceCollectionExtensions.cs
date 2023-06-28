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
using Steeltoe.Connectors.PostgreSql.DynamicTypeAccess;

namespace Steeltoe.Connectors.PostgreSql;

public static class PostgreSqlServiceCollectionExtensions
{
    public static IServiceCollection AddPostgreSql(this IServiceCollection services, IConfiguration configuration)
    {
        return AddPostgreSql(services, configuration, null);
    }

    public static IServiceCollection AddPostgreSql(this IServiceCollection services, IConfiguration configuration,
        Action<ConnectorAddOptionsBuilder>? addAction)
    {
        return AddPostgreSql(services, configuration, PostgreSqlPackageResolver.Default, addAction);
    }

    private static IServiceCollection AddPostgreSql(this IServiceCollection services, IConfiguration configuration, PostgreSqlPackageResolver packageResolver,
        Action<ConnectorAddOptionsBuilder>? addAction)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(packageResolver);

        if (!ConnectorFactoryShim<PostgreSqlOptions>.IsRegistered(packageResolver.NpgsqlConnectionClass.Type, services))
        {
            var optionsBuilder = new ConnectorAddOptionsBuilder(
                (serviceProvider, serviceBindingName) => CreateConnection(serviceProvider, serviceBindingName, packageResolver),
                (serviceProvider, serviceBindingName) => CreateHealthContributor(serviceProvider, serviceBindingName, packageResolver))
            {
                CacheConnection = false,
                EnableHealthChecks = services.All(descriptor => descriptor.ServiceType != typeof(HealthCheckService))
            };

            addAction?.Invoke(optionsBuilder);

            IReadOnlySet<string> optionNames = ConnectorOptionsBinder.RegisterNamedOptions<PostgreSqlOptions>(services, configuration, "postgresql",
                optionsBuilder.EnableHealthChecks ? optionsBuilder.CreateHealthContributor : null);

            ConnectorFactoryShim<PostgreSqlOptions>.Register(packageResolver.NpgsqlConnectionClass.Type, services, optionNames, optionsBuilder.CreateConnection,
                optionsBuilder.CacheConnection);
        }

        return services;
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string serviceBindingName,
        PostgreSqlPackageResolver packageResolver)
    {
        ConnectorFactoryShim<PostgreSqlOptions> connectorFactoryShim =
            ConnectorFactoryShim<PostgreSqlOptions>.FromServiceProvider(serviceProvider, packageResolver.NpgsqlConnectionClass.Type);

        ConnectorShim<PostgreSqlOptions> connectorShim = connectorFactoryShim.Get(serviceBindingName);

        var connection = (DbConnection)connectorShim.GetConnection();
        string hostName = GetHostNameFromConnectionString(packageResolver, connectorShim.Options.ConnectionString);
        var logger = serviceProvider.GetRequiredService<ILogger<RelationalDatabaseHealthContributor>>();

        return new RelationalDatabaseHealthContributor(connection, hostName, logger)
        {
            ServiceName = serviceBindingName
        };
    }

    private static string GetHostNameFromConnectionString(PostgreSqlPackageResolver packageResolver, string? connectionString)
    {
        var connectionStringBuilderShim = NpgsqlConnectionStringBuilderShim.CreateInstance(packageResolver);
        connectionStringBuilderShim.Instance.ConnectionString = connectionString;
        return (string)connectionStringBuilderShim.Instance["host"];
    }

    private static DbConnection CreateConnection(IServiceProvider serviceProvider, string serviceBindingName, PostgreSqlPackageResolver packageResolver)
    {
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<PostgreSqlOptions>>();
        PostgreSqlOptions options = optionsMonitor.Get(serviceBindingName);

        var npgsqlConnectionShim = NpgsqlConnectionShim.CreateInstance(packageResolver, options.ConnectionString);
        return npgsqlConnectionShim.Instance;
    }
}
