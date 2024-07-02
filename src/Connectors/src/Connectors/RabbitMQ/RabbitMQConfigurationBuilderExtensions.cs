// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Connectors.RabbitMQ;

public static class RabbitMQConfigurationBuilderExtensions
{
    /// <summary>
    /// Configures the connection string for a RabbitMQ server by merging settings from appsettings.json with cloud service bindings.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <returns>
    /// The <see cref="IConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder ConfigureRabbitMQ(this IConfigurationBuilder builder)
    {
        return ConfigureRabbitMQ(builder, null);
    }

    /// <summary>
    /// Configures the connection string for a RabbitMQ server by merging settings from appsettings.json with cloud service bindings.
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
    public static IConfigurationBuilder ConfigureRabbitMQ(this IConfigurationBuilder builder, Action<ConnectorConfigureOptionsBuilder>? configureAction)
    {
        ArgumentGuard.NotNull(builder);

        ConnectorConfigurer.Configure(builder, configureAction, new RabbitMQConnectionStringPostProcessor());
        return builder;
    }
}
