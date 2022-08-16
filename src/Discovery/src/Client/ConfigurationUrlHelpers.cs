// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Discovery.Client;

public static class ConfigurationUrlHelpers
{
    public const string WildcardHost = "---asterisk---";

    public static IList<Uri> GetAspNetCoreUrls(this IConfiguration config)
    {
        string urls = config["urls"];
        var uris = new List<Uri>();

        if (!string.IsNullOrEmpty(urls))
        {
            string[] addresses = urls.Split(';');

            foreach (string address in addresses)
            {
                if (!Uri.TryCreate(address, UriKind.Absolute, out Uri uri) && (address.Contains("*") || address.Contains("::") || address.Contains("+")))
                {
                    Uri.TryCreate(address.Replace("*", WildcardHost).Replace("::", $"{WildcardHost}:").Replace("+", $"{WildcardHost}"), UriKind.Absolute,
                        out uri);
                }

                uris.Add(uri);
            }
        }

        return uris;
    }
}
