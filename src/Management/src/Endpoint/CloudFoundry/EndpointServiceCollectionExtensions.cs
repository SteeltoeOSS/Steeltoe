// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

public static class EndpointServiceCollectionExtensions
{
    public static void AddCloudFoundryActuator(this IServiceCollection services, IConfiguration configuration = null)
    {
        ArgumentGuard.NotNull(services);

        configuration ??= services.BuildServiceProvider().GetRequiredService<IConfiguration>();

        services.AddCloudFoundryActuatorServices(configuration);
        services.AddActuatorEndpointMapping<CloudFoundryEndpoint>();
    }
}
