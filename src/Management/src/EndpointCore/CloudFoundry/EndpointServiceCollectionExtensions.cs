// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

public static class EndpointServiceCollectionExtensions
{
    public static void AddCloudFoundryActuator(this IServiceCollection services, IConfiguration config = null)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        config ??= services.BuildServiceProvider().GetService<IConfiguration>();

        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        services.AddCloudFoundryActuatorServices(config);
        services.AddActuatorEndpointMapping<CloudFoundryEndpoint>();
    }
}
