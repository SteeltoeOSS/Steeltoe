// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Configuration.CloudFoundry.ServiceBindings;
using Steeltoe.Connectors.SqlServer.RuntimeTypeAccess;
using IServiceBindingsReader = Steeltoe.Configuration.CloudFoundry.ServiceBindings.IServiceBindingsReader;

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
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder ConfigureSqlServer(this IConfigurationBuilder builder)
    {
        return ConfigureSqlServer(builder, null);
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
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder ConfigureSqlServer(this IConfigurationBuilder builder, Action<ConnectorConfigureOptionsBuilder>? configureAction)
    {
        return ConfigureSqlServer(builder, SqlServerPackageResolver.Default, configureAction, null);
    }

    internal static IConfigurationBuilder ConfigureSqlServer(this IConfigurationBuilder builder, SqlServerPackageResolver packageResolver,
        Action<ConnectorConfigureOptionsBuilder>? configureAction, IServiceBindingsReader? serviceBindingsReader)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(packageResolver);

        Action<ConnectorConfigureOptionsBuilder> overrideConfigureAction = options =>
        {
            configureAction?.Invoke(options);

            options.CloudFoundryBrokerTypes =
                options.SkipDefaultServiceBindings ? CloudFoundryServiceBrokerTypes.None : CloudFoundryServiceBrokerTypes.SqlServer;
        };

        ConnectorConfigurer.Configure(builder, overrideConfigureAction, new SqlServerConnectionStringPostProcessor(packageResolver), serviceBindingsReader,
            NullLoggerFactory.Instance);

        return builder;
    }
}
