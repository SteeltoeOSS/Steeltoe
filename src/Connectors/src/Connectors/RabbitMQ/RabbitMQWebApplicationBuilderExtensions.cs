// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.AspNetCore.Builder;
using Steeltoe.Common;
using Steeltoe.Connectors.RabbitMQ.DynamicTypeAccess;

namespace Steeltoe.Connectors.RabbitMQ;

public static class RabbitMQWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddRabbitMQ(this WebApplicationBuilder builder)
    {
        return AddRabbitMQ(builder, null);
    }

    public static WebApplicationBuilder AddRabbitMQ(this WebApplicationBuilder builder, Action<ConnectorSetupOptions>? setupAction)
    {
        return AddRabbitMQ(builder, new RabbitMQPackageResolver(), setupAction);
    }

    internal static WebApplicationBuilder AddRabbitMQ(this WebApplicationBuilder builder, RabbitMQPackageResolver packageResolver,
        Action<ConnectorSetupOptions>? setupAction = null)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        builder.Services.AddRabbitMQ(builder.Configuration, packageResolver, setupAction);
        return builder;
    }
}
