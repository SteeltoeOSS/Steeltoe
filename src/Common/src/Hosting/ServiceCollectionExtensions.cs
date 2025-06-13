// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace Steeltoe.Common.Hosting;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures <see cref="ForwardedHeadersOptions" /> to use forwarded headers as they are provided in Cloud Foundry. Includes
    /// <see cref="ForwardedHeaders.XForwardedHost" /> and <see cref="ForwardedHeaders.XForwardedProto" />, and adds known networks from environment
    /// variables.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to configure.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection" /> instance, for chaining.
    /// </returns>
    /// <remarks>
    /// Evaluates the environment variables CF_INSTANCE_IP and CF_INSTANCE_INTERNAL_IP, and if they contain valid IP addresses, adds their network to
    /// <see cref="ForwardedHeadersOptions.KnownNetworks" />.
    /// <para />
    /// IMPORTANT: <see cref="ForwardedHeadersExtensions.UseForwardedHeaders(IApplicationBuilder)" /> must be called separately to activate these options.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="services" /> is <c>null</c>.
    /// </exception>
    public static IServiceCollection ConfigureForwardedHeadersOptionsForCloudFoundry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions();

        services.AddOptions<ForwardedHeadersOptions>().Configure<IServiceProvider>((options, serviceProvider) =>
        {
            ILogger logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("Steeltoe.Common.Hosting.ServiceCollectionExtensions") ??
                NullLogger.Instance;

            options.ForwardedHeaders |= ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto;

            AddKnownNetworkFromEnv("CF_INSTANCE_IP", options, logger);
            AddKnownNetworkFromEnv("CF_INSTANCE_INTERNAL_IP", options, logger);
        });

        return services;
    }

    private static void AddKnownNetworkFromEnv(string envVar, ForwardedHeadersOptions options, ILogger logger)
    {
        string? ipString = Environment.GetEnvironmentVariable(envVar);

        if (IPAddress.TryParse(ipString, out IPAddress? address))
        {
            // Assume a /24 subnet mask (255.255.255.0), adjust if needed
            int prefixLength = address.AddressFamily == AddressFamily.InterNetwork ? 24 : 64;

            IPNetwork network = GetNetworkFromAddress(address, prefixLength);

            if (!options.KnownNetworks.Any(n => n.Prefix.Equals(network.Prefix) && n.PrefixLength == network.PrefixLength))
            {
                logger.LogDebug("Adding known network {Network}/{PrefixLength} from {EnvVar} to ForwardedHeadersOptions.", network.Prefix, network.PrefixLength,
                    envVar);

                options.KnownNetworks.Add(network);
            }
        }
    }

    internal static IPNetwork GetNetworkFromAddress(IPAddress address, int prefixLength)
    {
        byte[] bytes = address.GetAddressBytes();
        int fullBytes = prefixLength / 8;
        int remainingBits = prefixLength % 8;

        for (int i = fullBytes + (remainingBits > 0 ? 1 : 0); i < bytes.Length; i++)
        {
            bytes[i] = 0;
        }

        if (remainingBits > 0)
        {
            int mask = 0xFF << (8 - remainingBits);
            bytes[fullBytes] &= (byte)mask;
        }

        return new IPNetwork(new IPAddress(bytes), prefixLength);
    }
}
