// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Data.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.PostgreSql.RuntimeTypeAccess;
using Steeltoe.Connectors.RuntimeTypeAccess;

namespace Steeltoe.Connectors.PostgreSql;

public static class PostgreSqlWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddPostgreSql(this WebApplicationBuilder builder)
    {
        return AddPostgreSql(builder, new PostgreSqlPackageResolver());
    }

    private static WebApplicationBuilder AddPostgreSql(this WebApplicationBuilder builder, PostgreSqlPackageResolver packageResolver)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        var connectionStringPostProcessor = new PostgreSqlConnectionStringPostProcessor(packageResolver);
        BaseWebApplicationBuilderExtensions.RegisterConfigurationSource(builder.Configuration, connectionStringPostProcessor);

        Func<IServiceProvider, string, IHealthContributor> createHealthContributor = (serviceProvider, serviceBindingName) =>
            CreateHealthContributor(serviceProvider, serviceBindingName, packageResolver);

        BaseWebApplicationBuilderExtensions.RegisterNamedOptions<PostgreSqlOptions>(builder, "postgresql", createHealthContributor);

        Func<PostgreSqlOptions, string, object> createConnection = (options, _) =>
            NpgsqlConnectionShim.CreateInstance(packageResolver, options.ConnectionString).Instance;

        ConnectorFactoryShim<PostgreSqlOptions>.Register(builder.Services, packageResolver.NpgsqlConnectionClass.Type, false, createConnection);

        return builder;
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string serviceBindingName,
        PostgreSqlPackageResolver packageResolver)
    {
        ConnectorFactoryShim<PostgreSqlOptions> connectorFactoryShim =
            ConnectorFactoryShim<PostgreSqlOptions>.FromServiceProvider(serviceProvider, packageResolver.NpgsqlConnectionClass.Type);

        ConnectorShim<PostgreSqlOptions> connectorShim = connectorFactoryShim.GetNamed(serviceBindingName);

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
}
