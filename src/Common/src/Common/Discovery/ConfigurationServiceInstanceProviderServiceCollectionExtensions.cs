// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Discovery;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigurationServiceInstanceProviderServiceCollectionExtensions
{
    /// <summary>
    /// Adds an IConfiguration-based <see cref="IServiceInstanceProvider"/> to the <see cref="IServiceCollection" />
    /// </summary>
    /// <param name="services">Your <see cref="IServiceCollection"/></param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="serviceLifetime">Lifetime of the <see cref="IServiceInstanceProvider"/></param>
    /// <returns>IServiceCollection for chaining</returns>
    public static IServiceCollection AddConfigurationDiscoveryClient(this IServiceCollection services, IConfiguration configuration, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        services.Add(new ServiceDescriptor(typeof(IServiceInstanceProvider), typeof(ConfigurationServiceInstanceProvider), serviceLifetime));
        services.AddOptions();
        services.Configure<List<ConfigurationServiceInstance>>(configuration.GetSection("discovery:services"));
        return services;
    }
}
