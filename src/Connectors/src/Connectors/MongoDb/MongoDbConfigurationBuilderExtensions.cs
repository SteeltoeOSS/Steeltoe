// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Connectors.MongoDb;

public static class MongoDbConfigurationBuilderExtensions
{
    /// <summary>
    /// Configures the connection string for a MongoDB database by merging settings from appsettings.json with cloud service bindings.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder ConfigureMongoDb(this IConfigurationBuilder builder)
    {
        return ConfigureMongoDb(builder, null);
    }

    /// <summary>
    /// Configures the connection string for a MongoDB database by merging settings from appsettings.json with cloud service bindings.
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
    public static IConfigurationBuilder ConfigureMongoDb(this IConfigurationBuilder builder, Action<ConnectorConfigureOptionsBuilder>? configureAction)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ConnectorConfigurer.Configure(builder, configureAction, new MongoDbConnectionStringPostProcessor());
        return builder;
    }
}
