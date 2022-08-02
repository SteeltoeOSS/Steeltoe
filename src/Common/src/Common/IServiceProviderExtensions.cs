// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Common;

public static class ServiceProviderExtensions
{
    /// <summary>
    /// If an instance of <see cref="IApplicationInstanceInfo" /> is found, it is returned. Otherwise a default instance is returned.
    /// </summary>
    /// <param name="sp">
    /// Provider of services.
    /// </param>
    /// <returns>
    /// An instance of <see cref="IApplicationInstanceInfo" />.
    /// </returns>
    public static IApplicationInstanceInfo GetApplicationInstanceInfo(this IServiceProvider sp)
    {
        var appInfo = sp.GetService<IApplicationInstanceInfo>();

        if (appInfo == null)
        {
            var config = sp.GetRequiredService<IConfiguration>();
            appInfo = new ApplicationInstanceInfo(config, string.Empty);
        }

        return appInfo;
    }
}
