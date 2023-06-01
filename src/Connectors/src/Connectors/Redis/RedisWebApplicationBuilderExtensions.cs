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
        return AddRedis(builder, null);
    }

    public static WebApplicationBuilder AddRedis(this WebApplicationBuilder builder, Action<ConnectorSetupOptions>? setupAction)
    {
        ArgumentGuard.NotNull(builder);

        builder.Configuration.ConfigureRedis();
        builder.Services.AddRedis(builder.Configuration, setupAction);
        return builder;
    }
}
