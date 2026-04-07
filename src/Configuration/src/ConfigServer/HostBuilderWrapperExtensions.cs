// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Hosting;

namespace Steeltoe.Configuration.ConfigServer;

internal static class HostBuilderWrapperExtensions
{
    public static HostBuilderWrapper AddConfigServer(this HostBuilderWrapper wrapper, Action<ConfigServerClientOptions>? configure,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(wrapper);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        wrapper.ConfigureAppConfiguration((context, builder) =>
        {
            Action<ConfigServerClientOptions> configureOptions = CreateOptionsConfigurer(configure, context.HostEnvironment);
            builder.AddConfigServer(configureOptions, loggerFactory);
        });

        wrapper.ConfigureServices((context, services) =>
        {
            Action<ConfigServerClientOptions> configureOptions = CreateOptionsConfigurer(configure, context.HostEnvironment);
            services.AddConfigServerServices(configureOptions);
        });

        return wrapper;
    }

    private static Action<ConfigServerClientOptions> CreateOptionsConfigurer(Action<ConfigServerClientOptions>? configure, IHostEnvironment hostEnvironment)
    {
        return options =>
        {
            configure?.Invoke(options);

            if (!string.IsNullOrEmpty(hostEnvironment.EnvironmentName))
            {
                options.Environment ??= hostEnvironment.EnvironmentName;
            }
        };
    }
}
