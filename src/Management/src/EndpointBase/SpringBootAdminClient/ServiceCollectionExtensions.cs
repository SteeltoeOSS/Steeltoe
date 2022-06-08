// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.SpringBootAdminClient;
using System;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ServiceCollectionExtensions
{
    /// <summary>
    /// Register startup/shutdown interactions with Spring Boot Admin server
    /// </summary>
    /// <param name="services">Reference to the service collection</param>
    /// <returns>A reference to the service collection</returns>
    public static IServiceCollection AddSpringBootAdminClient(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.RegisterDefaultApplicationInstanceInfo();
        services.TryAddSingleton<ManagementEndpointOptions>();
        services.TryAddSingleton<HealthEndpointOptions>();
        services.AddSingleton<SpringBootAdminClientOptions>();
        services.AddHostedService<SpringBootAdminClientHostedService>();
        return services;
    }
}
