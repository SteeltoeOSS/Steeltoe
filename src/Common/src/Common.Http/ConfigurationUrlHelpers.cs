// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Common.Http;

public static class ConfigurationUrlHelpers
{
    private const string IPV4WildcardHost = "0.0.0.0";
    private const string IPV6WildcardHost = "[::]";

    private static readonly string[] MicrosoftWildcardHosts =
    {
        "*",
        "+"
    };

    public static readonly string[] WildcardHosts =
    {
        IPV4WildcardHost,
        IPV6WildcardHost
    };

    public static List<Uri> GetAspNetCoreUrls(this IConfiguration configuration)
    {
        string urlStringFromConfiguration = configuration["urls"];
        var foundUris = new List<Uri>();

        if (!string.IsNullOrEmpty(urlStringFromConfiguration))
        {
            string[] addresses = urlStringFromConfiguration.Split(new[]
            {
                ';'
            }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string address in addresses)
            {
                foundUris.AddNewUrisToList(address);
            }
        }

        return foundUris;
    }

    private static void AddNewUrisToList(this List<Uri> addresses, string address)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out Uri uri) &&
            address.Any(a => MicrosoftWildcardHosts.Contains(a.ToString(CultureInfo.InvariantCulture))))
        {
            addresses.AddUniqueUri(address.WildcardUriParse(MicrosoftWildcardHosts, IPV4WildcardHost));
            addresses.AddUniqueUri(address.WildcardUriParse(MicrosoftWildcardHosts, IPV6WildcardHost));
        }
        else
        {
            addresses.AddUniqueUri(uri);
        }
    }

    private static void AddUniqueUri(this List<Uri> addresses, Uri newUri)
    {
        if (!addresses.Contains(newUri) && !string.IsNullOrEmpty(newUri?.ToString()))
        {
            addresses.Add(newUri);
        }
    }

    private static Uri WildcardUriParse(this string source, string[] stringsToReplace, string replaceWith)
    {
        foreach (string currentString in stringsToReplace)
        {
            source = source.Replace(currentString, replaceWith, StringComparison.Ordinal);
        }

        return new Uri(source, UriKind.Absolute);
    }
}
