// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connectors.PostgreSql.DynamicTypeAccess;

namespace Steeltoe.Connectors.PostgreSql;

public static class PostgreSqlConfigurationBuilderExtensions
{
    /// <summary>
    /// Configures the connection string for a PostgreSQL database by merging settings from appsettings.json with cloud service bindings.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder ConfigurePostgreSql(this IConfigurationBuilder builder)
    {
        return ConfigurePostgreSql(builder, PostgreSqlPackageResolver.Default);
    }

    /// <summary>
    /// Configures the connection string for a PostgreSQL database by merging settings from appsettings.json with cloud service bindings.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <param name="configureAction">
    /// An optional delegate to configure this connector.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder ConfigurePostgreSql(this IConfigurationBuilder builder, Action<ConnectorConfigureOptionsBuilder>? configureAction)
    {
        return ConfigurePostgreSql(builder, PostgreSqlPackageResolver.Default, configureAction);
    }

    private static IConfigurationBuilder ConfigurePostgreSql(this IConfigurationBuilder builder, PostgreSqlPackageResolver packageResolver,
        Action<ConnectorConfigureOptionsBuilder>? configureAction = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(packageResolver);

        ConnectorConfigurer.Configure(builder, configureAction, new PostgreSqlConnectionStringPostProcessor(packageResolver));
        return builder;
    }
}
