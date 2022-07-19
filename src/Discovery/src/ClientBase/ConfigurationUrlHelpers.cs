// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Steeltoe.Discovery.Client;

public static class ConfigurationUrlHelpers
{
    public const string WildcardHost = "---asterisk---";

    public static List<Uri> GetAspNetCoreUrls(this IConfiguration config)
    {
        var urls = config["urls"];
        var uris = new List<Uri>();
        if (!string.IsNullOrEmpty(urls))
        {
            var addresses = urls.Split(';');
            foreach (var address in addresses)
            {
                if (!Uri.TryCreate(address, UriKind.Absolute, out var uri)
                    && (address.Contains("*") || address.Contains("::") || address.Contains("+")))
                {
                    Uri.TryCreate(address.Replace("*", WildcardHost).Replace("::", $"{WildcardHost}:").Replace("+", $"{WildcardHost}"), UriKind.Absolute, out uri);
                }

                uris.Add(uri);
            }
        }

        return uris;
    }
}
