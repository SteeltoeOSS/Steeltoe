// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Extensions.Logging;

namespace Steeltoe.Management.Kubernetes
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder AddKubernetesActuators(this IHostBuilder hostBuilder)
        {
            return hostBuilder
                .ConfigureLogging((context, configureLogging) => configureLogging.AddDynamicConsole(true))
                .ConfigureServices((context, collection) =>
                {
                    collection.AddKubernetesActuators();
                    collection.AddSingleton<IStartupFilter>(new KubernetesActuatorsStartupFilter());
                });
        }

        public static IWebHostBuilder AddKubernetesActuators(this IWebHostBuilder webHostBuilder)
        {
            return webHostBuilder
                .ConfigureLogging((context, configureLogging) => configureLogging.AddDynamicConsole(true))
                .ConfigureServices((context, collection) =>
                {
                    collection.AddKubernetesActuators();
                    collection.AddSingleton<IStartupFilter>(new KubernetesActuatorsStartupFilter());
                });
        }
    }
}
