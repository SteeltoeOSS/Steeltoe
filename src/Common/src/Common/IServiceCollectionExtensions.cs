// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Linq;

namespace Steeltoe.Common
{
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Try to register a default instance of <see cref="IApplicationInstanceInfo" />
        /// </summary>
        /// <param name="serviceCollection">Collection of configured services</param>
        public static void RegisterDefaultApplicationInstanceInfo(this IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddSingleton<IApplicationInstanceInfo>(services => new ApplicationInstanceInfo(services.GetRequiredService<IConfiguration>(), true));
        }

        /// <summary>
        /// If an instance of <see cref="IApplicationInstanceInfo"/> is found, it is returned.
        /// Otherwise a default instance is added to the collection and then returned.
        /// </summary>
        /// <param name="serviceCollection">Collection of configured services</param>
        /// <returns>Relevant <see cref="IApplicationInstanceInfo" /></returns>
        [Obsolete("This method builds a temporary service provider and should not be used")]
        public static IApplicationInstanceInfo GetApplicationInstanceInfo(this IServiceCollection serviceCollection)
        {
            var sp = serviceCollection.BuildServiceProvider();
            var appInfo = sp.GetServices<IApplicationInstanceInfo>();
            if (!appInfo.Any())
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var newAppInfo = new ApplicationInstanceInfo(config, true);
                serviceCollection.AddSingleton(typeof(IApplicationInstanceInfo), newAppInfo);
                return newAppInfo;
            }
            else
            {
                return appInfo.First();
            }
        }
    }
}
