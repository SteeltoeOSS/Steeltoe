// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Net;

public class InetOptions
{
    public const string Prefix = "spring:cloud:inet";

    public string DefaultHostname { get; set; } = "localhost";

    public string DefaultIpAddress { get; set; } = "127.0.0.1";

    public string IgnoredInterfaces { get; set; }

    public bool UseOnlySiteLocalInterfaces { get; set; }

    public string PreferredNetworks { get; set; }

    public bool SkipReverseDnsLookup { get; set; }

    internal IEnumerable<string> GetIgnoredInterfaces()
    {
        if (string.IsNullOrEmpty(IgnoredInterfaces))
        {
            return new List<string>();
        }

        return IgnoredInterfaces.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
    }

    internal IEnumerable<string> GetPreferredNetworks()
    {
        if (string.IsNullOrEmpty(PreferredNetworks))
        {
            return new List<string>();
        }

        return PreferredNetworks.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
    }
}
