// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;

namespace Steeltoe.Connectors.RabbitMQ;

public static class RabbitMQHostApplicationBuilderExtensions
{
    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> (with type parameters <see cref="RabbitMQOptions" /> and
    /// RabbitMQ.Client.IConnection) to connect to a RabbitMQ server.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddRabbitMQ(this IHostApplicationBuilder builder)
    {
        return AddRabbitMQ(builder, null, null);
    }

    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> (with type parameters <see cref="RabbitMQOptions" /> and
    /// RabbitMQ.Client.IConnection) to connect to a RabbitMQ server.
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
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddRabbitMQ(this IHostApplicationBuilder builder, Action<ConnectorConfigureOptionsBuilder>? configureAction,
        Action<ConnectorAddOptionsBuilder>? addAction)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Configuration.ConfigureRabbitMQ(configureAction);
        builder.Services.AddRabbitMQ(builder.Configuration, addAction);
        return builder;
    }
}
