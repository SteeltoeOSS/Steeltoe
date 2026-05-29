// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Configuration.CloudFoundry.ServiceBindings;

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
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
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
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder ConfigureRabbitMQ(this IConfigurationBuilder builder, Action<ConnectorConfigureOptionsBuilder>? configureAction)
    {
        return ConfigureRabbitMQ(builder, configureAction, null);
    }

    internal static IConfigurationBuilder ConfigureRabbitMQ(this IConfigurationBuilder builder, Action<ConnectorConfigureOptionsBuilder>? configureAction,
        IServiceBindingsReader? serviceBindingsReader)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Action<ConnectorConfigureOptionsBuilder> overrideConfigureAction = options =>
        {
            configureAction?.Invoke(options);

            options.CloudFoundryBrokerTypes =
                options.SkipDefaultServiceBindings ? CloudFoundryServiceBrokerTypes.None : CloudFoundryServiceBrokerTypes.RabbitMQ;
        };

        ConnectorConfigurer.Configure(builder, overrideConfigureAction, new RabbitMQConnectionStringPostProcessor(), serviceBindingsReader,
            NullLoggerFactory.Instance);

        return builder;
    }
}
