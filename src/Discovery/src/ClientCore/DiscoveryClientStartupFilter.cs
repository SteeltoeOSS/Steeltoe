// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Steeltoe.Discovery.Client;

/// <summary>
/// Provides an alternate means of activating the Discovery client. Use this filter in place of calling UseDiscoveryClient in Startup.Configure().
/// </summary>
[Obsolete("This functionality is now handled by DiscoveryClientService, this class will be removed in a future release")]
public class DiscoveryClientStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            next(app);

            app.UseDiscoveryClient();
        };
    }
}
