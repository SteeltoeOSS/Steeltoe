// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.AspNetCore.Builder;
using Steeltoe.Common;
using Steeltoe.Connectors.Redis.DynamicTypeAccess;

namespace Steeltoe.Connectors.Redis;

public static class RedisWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddRedis(this WebApplicationBuilder builder)
    {
        return AddRedis(builder, null);
    }

    public static WebApplicationBuilder AddRedis(this WebApplicationBuilder builder, Action<ConnectorSetupOptions>? setupAction)
    {
        return AddRedis(builder, new StackExchangeRedisPackageResolver(), new MicrosoftRedisPackageResolver(), setupAction);
    }

    internal static WebApplicationBuilder AddRedis(this WebApplicationBuilder builder, StackExchangeRedisPackageResolver stackExchangeRedisPackageResolver,
        MicrosoftRedisPackageResolver microsoftRedisPackageResolver, Action<ConnectorSetupOptions>? setupAction = null)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(stackExchangeRedisPackageResolver);
        ArgumentGuard.NotNull(microsoftRedisPackageResolver);

        builder.Services.AddRedis(builder.Configuration, stackExchangeRedisPackageResolver, microsoftRedisPackageResolver, setupAction);
        return builder;
    }
}
