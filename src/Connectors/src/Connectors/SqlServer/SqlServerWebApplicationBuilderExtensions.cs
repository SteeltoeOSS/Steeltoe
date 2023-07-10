// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Steeltoe.Common;
using Steeltoe.Connectors.SqlServer.RuntimeTypeAccess;

namespace Steeltoe.Connectors.SqlServer;

public static class SqlServerWebApplicationBuilderExtensions
{
    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> to connect to a SQL Server database.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="WebApplicationBuilder" /> to add services to.
    /// </param>
    /// <returns>
    /// The <see cref="WebApplicationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static WebApplicationBuilder AddSqlServer(this WebApplicationBuilder builder)
    {
        return AddSqlServer(builder, SqlServerPackageResolver.Default);
    }

    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> to connect to a SQL Server database.
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
    public static WebApplicationBuilder AddSqlServer(this WebApplicationBuilder builder, Action<ConnectorConfigureOptionsBuilder>? configureAction,
        Action<ConnectorAddOptionsBuilder>? addAction)
    {
        return AddSqlServer(builder, SqlServerPackageResolver.Default, configureAction, addAction);
    }

    internal static WebApplicationBuilder AddSqlServer(this WebApplicationBuilder builder, SqlServerPackageResolver packageResolver,
        Action<ConnectorConfigureOptionsBuilder>? configureAction = null, Action<ConnectorAddOptionsBuilder>? addAction = null)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        builder.Configuration.ConfigureSqlServer(packageResolver, configureAction);
        builder.Services.AddSqlServer(builder.Configuration, packageResolver, addAction);
        return builder;
    }
}
