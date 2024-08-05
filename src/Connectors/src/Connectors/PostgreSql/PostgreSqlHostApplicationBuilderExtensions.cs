// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;

namespace Steeltoe.Connectors.PostgreSql;

public static class PostgreSqlHostApplicationBuilderExtensions
{
    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> (with type parameters <see cref="PostgreSqlOptions" /> and Npgsql.NpgsqlConnection)
    /// to connect to a PostgreSQL database.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The <see cref="IHostApplicationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddPostgreSql(this IHostApplicationBuilder builder)
    {
        return AddPostgreSql(builder, null, null);
    }

    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> (with type parameters <see cref="PostgreSqlOptions" /> and Npgsql.NpgsqlConnection)
    /// to connect to a PostgreSQL database.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <param name="configureAction">
    /// An optional delegate to configure configuration of this connector.
    /// </param>
    /// <param name="addAction">
    /// An optional delegate to configure this connector.
    /// </param>
    /// <returns>
    /// The <see cref="IHostApplicationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddPostgreSql(this IHostApplicationBuilder builder, Action<ConnectorConfigureOptionsBuilder>? configureAction,
        Action<ConnectorAddOptionsBuilder>? addAction)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Configuration.ConfigurePostgreSql(configureAction);
        builder.Services.AddPostgreSql(builder.Configuration, addAction);
        return builder;
    }
}
