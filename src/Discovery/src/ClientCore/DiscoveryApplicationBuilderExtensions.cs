// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Steeltoe.Discovery.Client;

public static class DiscoveryApplicationBuilderExtensions
{
    /// <summary>
    /// Activates the configured <see cref="IDiscoveryClient"/> and registers shutdown events via <see cref="IDiscoveryLifecycle"/>.
    /// </summary>
    [Obsolete("This method call is no longer needed, you should remove it.")]
    public static IApplicationBuilder UseDiscoveryClient(this IApplicationBuilder app)
    {
        _ = app.ApplicationServices.GetRequiredService<IDiscoveryClient>();

        // make sure that the lifecycle object is created
        _ = app.ApplicationServices.GetService<IDiscoveryLifecycle>();
        return app;
    }
}
