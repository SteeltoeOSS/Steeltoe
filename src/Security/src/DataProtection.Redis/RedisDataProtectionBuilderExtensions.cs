// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Steeltoe.Connectors;
using Steeltoe.Connectors.Redis;

namespace Steeltoe.Security.DataProtection.Redis;

public static class RedisDataProtectionBuilderExtensions
{
    /// <summary>
    /// Configures the data protection system to persist keys in a Redis database, using the Steeltoe Connector for Redis.
    /// </summary>
    /// <param name="builder">
    /// The builder instance to modify.
    /// </param>
    /// <returns>
    /// A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.
    /// </returns>
    public static IDataProtectionBuilder PersistKeysToRedis(this IDataProtectionBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton(provider =>
        {
            var connectorFactory = provider.GetRequiredService<ConnectorFactory<RedisOptions, IDistributedCache>>();
            Connector<RedisOptions, IDistributedCache> connector = connectorFactory.Get();
            return connector.GetConnection();
        });

        builder.Services.TryAddSingleton<IXmlRepository, CloudFoundryRedisXmlRepository>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<KeyManagementOptions>, ConfigureKeyManagementOptions>());

        return builder;
    }
}
