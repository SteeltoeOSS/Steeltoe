// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Web.Hypermedia;

public static class EndpointServiceCollectionExtensions
{
    public static void AddHypermediaActuator(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        services.AddCommonActuatorServices();
        services.AddHypermediaActuatorServices();
    }
}
