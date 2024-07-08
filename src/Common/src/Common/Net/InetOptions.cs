// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Steeltoe.Common.Net;

/// <summary>
/// Provides settings for resolving network interfaces.
/// </summary>
public sealed class InetOptions
{
    internal const string ConfigurationPrefix = "spring:cloud:inet";

    /// <summary>
    /// Gets or sets the default hostname. Default value: localhost.
    /// </summary>
    public string? DefaultHostname { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the default IP address. Default value: 127.0.0.1.
    /// </summary>
    public string? DefaultIPAddress { get; set; } = "127.0.0.1";

    /// <summary>
    /// Gets or sets a comma-delimited list of network interfaces to ignore.
    /// </summary>
    public string? IgnoredInterfaces { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use only site-local network interfaces.
    /// </summary>
    public bool UseOnlySiteLocalInterfaces { get; set; }

    /// <summary>
    /// Gets or sets a comma-delimited list of preferred networks.
    /// </summary>
    public string? PreferredNetworks { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to skip reverse DNS lookups.
    /// </summary>
    public bool SkipReverseDnsLookup { get; set; }

    internal IEnumerable<string> GetIgnoredInterfaces()
    {
        if (string.IsNullOrEmpty(IgnoredInterfaces))
        {
            return [];
        }

        return IgnoredInterfaces.Split(',', StringSplitOptions.RemoveEmptyEntries);
    }

    internal IEnumerable<string> GetPreferredNetworks()
    {
        if (string.IsNullOrEmpty(PreferredNetworks))
        {
            return [];
        }

        return PreferredNetworks.Split(',', StringSplitOptions.RemoveEmptyEntries);
    }
}
