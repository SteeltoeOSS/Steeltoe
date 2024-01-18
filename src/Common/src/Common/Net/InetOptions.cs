// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Steeltoe.Common.Net;

public sealed class InetOptions
{
    internal const string ConfigurationPrefix = "spring:cloud:inet";

    public string? DefaultHostname { get; set; } = "localhost";
    public string? DefaultIPAddress { get; set; } = "127.0.0.1";
    public string? IgnoredInterfaces { get; set; }
    public bool UseOnlySiteLocalInterfaces { get; set; }
    public string? PreferredNetworks { get; set; }
    public bool SkipReverseDnsLookup { get; set; }

    internal IEnumerable<string> GetIgnoredInterfaces()
    {
        if (string.IsNullOrEmpty(IgnoredInterfaces))
        {
            return Enumerable.Empty<string>();
        }

        return IgnoredInterfaces.Split(',', StringSplitOptions.RemoveEmptyEntries);
    }

    internal IEnumerable<string> GetPreferredNetworks()
    {
        if (string.IsNullOrEmpty(PreferredNetworks))
        {
            return Enumerable.Empty<string>();
        }

        return PreferredNetworks.Split(',', StringSplitOptions.RemoveEmptyEntries);
    }
}
