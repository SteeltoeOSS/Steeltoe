// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using Steeltoe.Connectors.MySql.DynamicTypeAccess;

namespace Steeltoe.Connectors.MySql;

public static class MySqlHostApplicationBuilderExtensions
{
    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> (with type parameters <see cref="MySqlOptions" /> and
    /// MySqlConnector.MySqlConnection or MySql.Data.MySqlClient.MySqlConnection) to connect to a MySQL compatible database.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The <see cref="IHostApplicationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddMySql(this IHostApplicationBuilder builder)
    {
        return AddMySql(builder, MySqlPackageResolver.Default);
    }

    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> (with type parameters <see cref="MySqlOptions" /> and
    /// MySqlConnector.MySqlConnection or MySql.Data.MySqlClient.MySqlConnection) to connect to a MySQL compatible database.
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
    public static IHostApplicationBuilder AddMySql(this IHostApplicationBuilder builder, Action<ConnectorConfigureOptionsBuilder>? configureAction,
        Action<ConnectorAddOptionsBuilder>? addAction)
    {
        return AddMySql(builder, MySqlPackageResolver.Default, configureAction, addAction);
    }

    internal static IHostApplicationBuilder AddMySql(this IHostApplicationBuilder builder, MySqlPackageResolver packageResolver,
        Action<ConnectorConfigureOptionsBuilder>? configureAction = null, Action<ConnectorAddOptionsBuilder>? addAction = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(packageResolver);

        builder.Configuration.ConfigureMySql(packageResolver, configureAction);
        builder.Services.AddMySql(builder.Configuration, packageResolver, addAction);
        return builder;
    }
}
