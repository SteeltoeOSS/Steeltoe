// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.AspNetCore.Builder;
using Steeltoe.Common;

namespace Steeltoe.Connectors.Redis;

public static class RedisWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddRedis(this WebApplicationBuilder builder)
    {
        return AddRedis(builder, null, null);
    }

    public static WebApplicationBuilder AddRedis(this WebApplicationBuilder builder, Action<ConnectorConfigureOptionsBuilder>? configureAction,
        Action<ConnectorAddOptionsBuilder>? addAction)
    {
        ArgumentGuard.NotNull(builder);

        builder.Configuration.ConfigureRedis(configureAction);
        builder.Services.AddRedis(builder.Configuration, addAction);
        return builder;
    }
}
