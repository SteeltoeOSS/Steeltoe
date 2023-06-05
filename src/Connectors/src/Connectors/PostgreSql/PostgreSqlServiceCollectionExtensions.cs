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
using Steeltoe.Connectors.DynamicTypeAccess;
using Steeltoe.Connectors.PostgreSql.DynamicTypeAccess;

namespace Steeltoe.Connectors.PostgreSql;

public static class PostgreSqlServiceCollectionExtensions
{
    public static IServiceCollection AddPostgreSql(this IServiceCollection services, IConfiguration configuration)
    {
        return AddPostgreSql(services, configuration, null);
    }

    public static IServiceCollection AddPostgreSql(this IServiceCollection services, IConfiguration configuration, Action<ConnectorAddOptions>? addAction)
    {
        return AddPostgreSql(services, configuration, PostgreSqlPackageResolver.Default, addAction);
    }

    private static IServiceCollection AddPostgreSql(this IServiceCollection services, IConfiguration configuration, PostgreSqlPackageResolver packageResolver,
        Action<ConnectorAddOptions>? addAction)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(packageResolver);

        var addOptions = new ConnectorAddOptions(
            (serviceProvider, serviceBindingName) => CreateConnection(serviceProvider, serviceBindingName, packageResolver),
            (serviceProvider, serviceBindingName) => CreateHealthContributor(serviceProvider, serviceBindingName, packageResolver))
        {
            CacheConnection = false
        };

        addAction?.Invoke(addOptions);

        IReadOnlySet<string> optionNames = ConnectorOptionsBinder.RegisterNamedOptions<PostgreSqlOptions>(services, configuration, "postgresql",
            addOptions.EnableHealthChecks ? addOptions.CreateHealthContributor : null);

        ConnectorFactoryShim<PostgreSqlOptions>.Register(packageResolver.NpgsqlConnectionClass.Type, services, optionNames, addOptions.CreateConnection,
            addOptions.CacheConnection);

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
        var logger = serviceProvider.GetRequiredService<ILogger<RelationalDbHealthContributor>>();

        return new RelationalDbHealthContributor(connection, $"PostgreSQL-{serviceBindingName}", hostName, logger);
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
