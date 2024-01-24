// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Http;
using Steeltoe.Common.Net;
using Steeltoe.Discovery.Consul.Discovery;

namespace Steeltoe.Discovery.Consul;

internal static class ConsulPostConfigurer
{
    /// <summary>
    /// At PostConfigure, confirm that settings are valid for the current environment.
    /// </summary>
    public static void ValidateConsulOptions(ConsulOptions options)
    {
        ArgumentGuard.NotNull(options);

        if ((Platform.IsContainerized || Platform.IsCloudHosted) && options.Host == "localhost")
        {
            throw new InvalidOperationException(
                $"Consul URL {options.Scheme}://{options.Host}:{options.Port} is not valid in containerized or cloud environments. Please configure Consul:Host with a non-localhost server.");
        }
    }

    /// <summary>
    /// Perform post-configuration on ConsulDiscoveryOptions.
    /// </summary>
    public static void UpdateDiscoveryOptions(IConfiguration configuration, ConsulDiscoveryOptions discoveryOptions, InetOptions inetOptions,
        ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(discoveryOptions);
        ArgumentGuard.NotNull(inetOptions);
        ArgumentGuard.NotNull(loggerFactory);

        ILogger<InetUtils> logger = loggerFactory.CreateLogger<InetUtils>();
        discoveryOptions.NetUtils = new InetUtils(inetOptions, logger);
        discoveryOptions.ApplyNetUtils();
        discoveryOptions.ApplyConfigUrls(configuration.GetAspNetCoreUrls());
    }
}
