// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Steeltoe.Extensions.Configuration;

public static class IServiceCollectionExtensions
{
    /// <summary>
    /// If an instance of <see cref="IServicesInfo"/> is found, it is returned.
    /// Otherwise a default instance is added to the collection and then returned.
    /// </summary>
    /// <param name="serviceCollection">Collection of configured services</param>
    /// <returns>Relevant <see cref="IServicesInfo" /></returns>
    [Obsolete("This method builds a temporary service provider and should not be used")]
    public static IServicesInfo GetServicesInfo(this IServiceCollection serviceCollection)
    {
        var sp = serviceCollection.BuildServiceProvider();
        var servicesInfo = sp.GetServices<IServicesInfo>();
        if (!servicesInfo.Any())
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var newServicesInfo = new ServicesOptions(config);
            serviceCollection.AddSingleton(typeof(IServicesInfo), newServicesInfo);
        }

        return servicesInfo.First();
    }
}