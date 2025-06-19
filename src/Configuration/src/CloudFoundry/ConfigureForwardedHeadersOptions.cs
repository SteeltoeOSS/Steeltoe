// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace Steeltoe.Configuration.CloudFoundry;

internal sealed class ConfigureForwardedHeadersOptions(IOptions<ForwardedHeadersSettings> headerSettings, ILogger<ConfigureForwardedHeadersOptions> logger)
    : IConfigureOptions<ForwardedHeadersOptions>
{
    public void Configure(ForwardedHeadersOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!Platform.IsCloudFoundry)
        {
            return;
        }

        options.ForwardedHeaders |= ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto;

        if (!IsDefaultKnownNetworks(options.KnownNetworks) || !IsDefaultKnownProxies(options.KnownProxies))
        {
            logger.LogTrace("Known proxies or networks have already been configured.");
            return;
        }

        if (headerSettings.Value.KnownNetworks?.Length > 0)
        {
            AddKnownNetworksFromConfiguration(options);
        }
        else if (headerSettings.Value.TrustAllNetworks)
        {
            logger.LogInformation(
                "'TrustAllNetworks' has been set, forwarded headers will be allowed from any source. This should only be used behind a trusted ingress.");

            options.KnownNetworks.Clear();
            options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("0.0.0.0"), 0));
        }
    }

    private static bool IsDefaultKnownNetworks(IList<IPNetwork> networks)
    {
        if (networks.Count != 1)
        {
            return false;
        }

        IPNetwork network = networks[0];

        return network is { PrefixLength: 8, Prefix.AddressFamily: AddressFamily.InterNetwork } &&
            network.Prefix.GetAddressBytes().SequenceEqual(IPAddress.Parse("127.0.0.1").GetAddressBytes());
    }

    private static bool IsDefaultKnownProxies(IList<IPAddress> proxies)
    {
        return proxies.Count == 1 && proxies[0].Equals(IPAddress.IPv6Loopback);
    }

    private void AddKnownNetworksFromConfiguration(ForwardedHeadersOptions options)
    {
        foreach (string cidr in headerSettings.Value.KnownNetworks?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [])
        {
            if (TryParseCidr(cidr, out IPNetwork network) && !options.KnownNetworks.Any(knownNetwork =>
                knownNetwork.Prefix.Equals(network.Prefix) && knownNetwork.PrefixLength.Equals(network.PrefixLength)))
            {
                logger.LogDebug("Adding known network {Network}/{PrefixLength} from configuration.", network.Prefix, network.PrefixLength);
                options.KnownNetworks.Add(network);
            }
            else
            {
                logger.LogWarning("Invalid CIDR format in {KnownNetworksKey}: '{CIDR}'.", $"{ForwardedHeadersSettings.ConfigurationKey}:KnownNetworks", cidr);
            }
        }
    }

    internal static bool TryParseCidr(string cidr, out IPNetwork network)
    {
        network = null!;
        string[] parts = cidr.Split('/');

        if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out IPAddress? address) ||
            !int.TryParse(parts[1], CultureInfo.InvariantCulture, out int prefixLength))
        {
            return false;
        }

        try
        {
            network = new IPNetwork(address, prefixLength);
            return true;
        }
        catch
        {
            // If the IPNetwork constructor throws, the CIDR is invalid.
        }

        return false;
    }
}
