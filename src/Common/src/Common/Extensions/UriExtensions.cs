// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;

namespace Steeltoe.Common.Extensions;

public static class UriExtensions
{
    public static string ToMaskedString(this Uri source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return source.ToMaskedUri().ToString();
    }

    public static Uri ToMaskedUri(this Uri source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (string.IsNullOrEmpty(source.UserInfo))
        {
            return source;
        }

        var builder = new UriBuilder(source)
        {
            UserName = "****",
            Password = "****"
        };

        return builder.Uri;
    }

    /// <summary>
    /// Parse a querystring into a dictionary of key value pairs.
    /// </summary>
    /// <param name="querystring">The querystring to parse.</param>
    /// <returns>Pairs of keys and values.</returns>
    public static Dictionary<string, string> ParseQuerystring(string querystring)
    {
        var result = new Dictionary<string, string>();
        foreach (var pair in querystring.Split('&'))
        {
            if (!string.IsNullOrEmpty(pair))
            {
                var kvp = pair.Split('=');
                result.Add(WebUtility.UrlDecode(kvp[0]), WebUtility.UrlDecode(kvp[1]));
            }
        }

        return result;
    }
}
