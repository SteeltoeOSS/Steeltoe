// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Steeltoe.Common;

namespace Steeltoe.Connectors.PostgreSql;

public static class PostgreSqlWebApplicationBuilderExtensions
{
    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> (with type parameters <see cref="PostgreSqlOptions" /> and Npgsql.NpgsqlConnection)
    /// to connect to a PostgreSQL database.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="WebApplicationBuilder" /> to add services to.
    /// </param>
    /// <returns>
    /// The <see cref="WebApplicationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static WebApplicationBuilder AddPostgreSql(this WebApplicationBuilder builder)
    {
        return AddPostgreSql(builder, null, null);
    }

    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> (with type parameters <see cref="PostgreSqlOptions" /> and Npgsql.NpgsqlConnection)
    /// to connect to a PostgreSQL database.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="WebApplicationBuilder" /> to add services to.
    /// </param>
    /// <param name="configureAction">
    /// An optional delegate to configure configuration of this connector.
    /// </param>
    /// <param name="addAction">
    /// An optional delegate to configure this connector.
    /// </param>
    /// <returns>
    /// The <see cref="WebApplicationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static WebApplicationBuilder AddPostgreSql(this WebApplicationBuilder builder, Action<ConnectorConfigureOptionsBuilder>? configureAction,
        Action<ConnectorAddOptionsBuilder>? addAction)
    {
        ArgumentGuard.NotNull(builder);

        builder.Configuration.ConfigurePostgreSql(configureAction);
        builder.Services.AddPostgreSql(builder.Configuration, addAction);
        return builder;
    }
}
