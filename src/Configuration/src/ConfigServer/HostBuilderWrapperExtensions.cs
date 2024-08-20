// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Hosting;
using Steeltoe.Common.Logging;

namespace Steeltoe.Configuration.ConfigServer;

internal static class HostBuilderWrapperExtensions
{
    public static HostBuilderWrapper AddConfigServer(this HostBuilderWrapper wrapper, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(wrapper);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        wrapper.ConfigureAppConfiguration((context, builder) =>
        {
            ConfigServerClientOptions options = CreateOptions(context.HostEnvironment);
            builder.AddConfigServer(options, loggerFactory);
        });

        wrapper.ConfigureServices(services => services.AddConfigServerServices());

        if (loggerFactory is IBootstrapLoggerFactory)
        {
            BootstrapLoggerHostedService.Register(wrapper);
        }

        return wrapper;
    }

    private static ConfigServerClientOptions CreateOptions(IHostEnvironment hostEnvironment)
    {
        var options = new ConfigServerClientOptions();

        if (!string.IsNullOrEmpty(hostEnvironment.EnvironmentName) && hostEnvironment.EnvironmentName != "Production")
        {
            // Only take IHostEnvironment.EnvironmentName when it was explicitly set (it defaults to "Production").
            // In the default case, we want the various other ways of setting the environment name to kick in.
            options.Environment = hostEnvironment.EnvironmentName;
        }

        // Intentionally NOT taking hostEnvironment.ApplicationName here, because that would disable the various other ways of setting the application name.
        // Ultimately, its value ends up in configuration key "applicationName", whose value is used if nothing else is configured.

        return options;
    }
}
