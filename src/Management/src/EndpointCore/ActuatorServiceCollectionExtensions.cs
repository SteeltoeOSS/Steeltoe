// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;

namespace Steeltoe.Management.Endpoint
{
    public static class ActuatorServiceCollectionExtensions
    {
        public static void RegisterEndpointOptions(this IServiceCollection services, IEndpointOptions options)
        {
            var mgmtOptions = services.BuildServiceProvider().GetServices<IManagementOptions>();
            foreach (var mgmtOption in mgmtOptions)
            {
                mgmtOption.EndpointOptions.Add(options);
            }
        }
    }
}
