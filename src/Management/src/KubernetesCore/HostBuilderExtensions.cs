// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint;

namespace Steeltoe.Management.KubernetesCore
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder AddKubernetesActuators(this IHostBuilder hostBuilder, MediaTypeVersion mediaTypeVersion = MediaTypeVersion.V2)
        {
            return hostBuilder
                .ConfigureLogging((context, configureLogging) => configureLogging.AddDynamicConsole(true))
                .ConfigureServices((context, collection) =>
                {
                    collection.AddKubernetesActuators();
                    collection.AddSingleton<IStartupFilter>(new KubernetesActuatorsStartupFilter(mediaTypeVersion));
                });
        }

        public static IWebHostBuilder AddKubernetesActuators(this IWebHostBuilder webHostBuilder, MediaTypeVersion mediaTypeVersion = MediaTypeVersion.V2)
        {
            return webHostBuilder
                .ConfigureLogging((context, configureLogging) => configureLogging.AddDynamicConsole(true))
                .ConfigureServices((context, collection) =>
                {
                    collection.AddKubernetesActuators();
                    collection.AddSingleton<IStartupFilter>(new KubernetesActuatorsStartupFilter(mediaTypeVersion));
                });
        }
    }
}
