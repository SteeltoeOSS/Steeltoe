// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Steeltoe.Management.Endpoint.Actuators.Metrics.Diagnostics;

internal static class ServiceCollectionExtensions
{
    public static void AddDiagnosticsManager(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IDiagnosticsManager, DiagnosticsManager>();
        services.AddHostedService<DiagnosticsService>();
    }
}
