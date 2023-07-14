// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Steeltoe.Common;

namespace Steeltoe.Connectors.RabbitMQ;

public static class RabbitMQWebApplicationBuilderExtensions
{
    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> (with type parameters <see cref="RabbitMQOptions" /> and
    /// RabbitMQ.Client.IConnection) to connect to a RabbitMQ server.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="WebApplicationBuilder" /> to add services to.
    /// </param>
    /// <returns>
    /// The <see cref="WebApplicationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static WebApplicationBuilder AddRabbitMQ(this WebApplicationBuilder builder)
    {
        return AddRabbitMQ(builder, null, null);
    }

    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> (with type parameters <see cref="RabbitMQOptions" /> and
    /// RabbitMQ.Client.IConnection) to connect to a RabbitMQ server.
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
    public static WebApplicationBuilder AddRabbitMQ(this WebApplicationBuilder builder, Action<ConnectorConfigureOptionsBuilder>? configureAction,
        Action<ConnectorAddOptionsBuilder>? addAction)
    {
        ArgumentGuard.NotNull(builder);

        builder.Configuration.ConfigureRabbitMQ(configureAction);
        builder.Services.AddRabbitMQ(builder.Configuration, addAction);
        return builder;
    }
}
