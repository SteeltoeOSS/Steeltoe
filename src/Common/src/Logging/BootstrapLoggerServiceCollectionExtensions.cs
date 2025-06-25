// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Common.Logging;

public static class BootstrapLoggerServiceCollectionExtensions
{
    /// <summary>
    /// Registers a hosted service that upgrades the loggers inside the specified bootstrap logger factory with new instances from the service container.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="bootstrapLoggerFactory">
    /// The bootstrap logger factory whose loggers to upgrade once the app has started.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection UpgradeBootstrapLoggerFactory(this IServiceCollection services, BootstrapLoggerFactory bootstrapLoggerFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(bootstrapLoggerFactory);

        services.AddHostedService(serviceProvider =>
        {
            var replacementLoggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            return new UpgradeBootstrapLoggerHostedService(bootstrapLoggerFactory, replacementLoggerFactory);
        });

        return services;
    }
}
