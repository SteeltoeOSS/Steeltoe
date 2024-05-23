// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.HeapDump;

public static class EndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds components of the Heap Dump actuator to the D/I container.
    /// </summary>
    /// <param name="services">
    /// Service collection to add actuator to.
    /// </param>
    public static void AddHeapDumpActuator(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        services.AddHeapDumpActuatorServices();

        services.TryAddSingleton<HeapDumper>();
        services.AddCommonActuatorServices();
    }
}
