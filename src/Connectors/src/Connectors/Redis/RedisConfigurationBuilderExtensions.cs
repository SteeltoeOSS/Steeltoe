// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Connectors.Redis;

public static class RedisConfigurationBuilderExtensions
{
    public static IConfigurationBuilder ConfigureRedis(this IConfigurationBuilder builder)
    {
        return ConfigureRedis(builder, null);
    }

    public static IConfigurationBuilder ConfigureRedis(this IConfigurationBuilder builder, Action<ConnectorConfigureOptions>? configureAction)
    {
        ArgumentGuard.NotNull(builder);

        ConnectorConfigurer.Configure(builder, configureAction, new RedisConnectionStringPostProcessor());
        return builder;
    }
}
