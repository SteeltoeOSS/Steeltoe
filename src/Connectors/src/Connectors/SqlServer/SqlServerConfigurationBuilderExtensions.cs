// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Connectors.SqlServer.RuntimeTypeAccess;

namespace Steeltoe.Connectors.SqlServer;

public static class SqlServerConfigurationBuilderExtensions
{
    /// <summary>
    /// Configures the connection string for a SQL Server database by merging settings from appsettings.json with cloud service bindings.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <returns>
    /// The <see cref="IConfigurationBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder ConfigureSqlServer(this IConfigurationBuilder builder)
    {
        return ConfigureSqlServer(builder, SqlServerPackageResolver.Default);
    }

    /// <summary>
    /// Configures the connection string for a SQL Server database by merging settings from appsettings.json with cloud service bindings.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <param name="configureAction">
    /// An optional delegate to configure this connector.
    /// </param>
    /// <returns>
    /// The <see cref="IConfigurationBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder ConfigureSqlServer(this IConfigurationBuilder builder, Action<ConnectorConfigureOptionsBuilder>? configureAction)
    {
        return ConfigureSqlServer(builder, SqlServerPackageResolver.Default, configureAction);
    }

    internal static IConfigurationBuilder ConfigureSqlServer(this IConfigurationBuilder builder, SqlServerPackageResolver packageResolver,
        Action<ConnectorConfigureOptionsBuilder>? configureAction = null)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        ConnectorConfigurer.Configure(builder, configureAction, new SqlServerConnectionStringPostProcessor(packageResolver));
        return builder;
    }
}
