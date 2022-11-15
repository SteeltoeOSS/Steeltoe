// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Discovery.Client;

public static class ConfigurationUrlHelpers
{
    public const string WildcardHost = "---asterisk---";

    public static List<Uri> GetAspNetCoreUrls(this IConfiguration configuration)
    {
        string urls = configuration["urls"];
        var uris = new List<Uri>();

        if (!string.IsNullOrEmpty(urls))
        {
            string[] addresses = urls.Split(';');

            foreach (string address in addresses)
            {
                if (!Uri.TryCreate(address, UriKind.Absolute, out Uri uri) && (address.Contains('*') ||
                    address.Contains("::", StringComparison.Ordinal) || address.Contains('+')))
                {
                    Uri.TryCreate(
                        address.Replace("*", WildcardHost, StringComparison.Ordinal).Replace("::", $"{WildcardHost}:", StringComparison.Ordinal)
                            .Replace("+", $"{WildcardHost}", StringComparison.Ordinal), UriKind.Absolute, out uri);
                }

                uris.Add(uri);
            }
        }

        return uris;
    }
}
