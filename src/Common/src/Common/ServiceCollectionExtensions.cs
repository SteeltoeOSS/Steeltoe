// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Steeltoe.Common;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Try to register a default instance of <see cref="IApplicationInstanceInfo" />.
    /// </summary>
    /// <param name="serviceCollection">
    /// Collection of configured services.
    /// </param>
    public static void RegisterDefaultApplicationInstanceInfo(this IServiceCollection serviceCollection)
    {
        serviceCollection.TryAddSingleton<IApplicationInstanceInfo>(services =>
            new ApplicationInstanceInfo(services.GetRequiredService<IConfiguration>(), true));
    }
}
