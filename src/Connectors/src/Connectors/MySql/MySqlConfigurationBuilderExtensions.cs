// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Connectors.MySql.DynamicTypeAccess;

namespace Steeltoe.Connectors.MySql;

public static class MySqlConfigurationBuilderExtensions
{
    /// <summary>
    /// Configures the connection string for a MySQL compatible database by merging settings from appsettings.json with cloud service bindings.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <returns>
    /// The <see cref="IConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder ConfigureMySql(this IConfigurationBuilder builder)
    {
        return ConfigureMySql(builder, MySqlPackageResolver.Default);
    }

    /// <summary>
    /// Configures the connection string for a MySQL compatible database by merging settings from appsettings.json with cloud service bindings.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <param name="configureAction">
    /// An optional delegate to configure this connector.
    /// </param>
    /// <returns>
    /// The <see cref="IConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder ConfigureMySql(this IConfigurationBuilder builder, Action<ConnectorConfigureOptionsBuilder>? configureAction)
    {
        return ConfigureMySql(builder, MySqlPackageResolver.Default, configureAction);
    }

    internal static IConfigurationBuilder ConfigureMySql(this IConfigurationBuilder builder, MySqlPackageResolver packageResolver,
        Action<ConnectorConfigureOptionsBuilder>? configureAction = null)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        ConnectorConfigurer.Configure(builder, configureAction, new MySqlConnectionStringPostProcessor(packageResolver));
        return builder;
    }
}
