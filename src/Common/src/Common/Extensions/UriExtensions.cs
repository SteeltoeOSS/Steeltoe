// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Steeltoe.Common.Extensions;

public static class UriExtensions
{
    private static readonly char[] _uriSeparatorChar = { ',' };

    /// <summary>
    /// Parse a querystring into a dictionary of key value pairs
    /// </summary>
    /// <param name="querystring">The querystring to parse</param>
    /// <returns>Pairs of keys and values</returns>
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

    public static string ToMaskedString(this Uri source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var uris = source.ToString();
        return string.Join(",", uris.Split(_uriSeparatorChar, StringSplitOptions.RemoveEmptyEntries).Select(uri => new Uri(uri).ToMaskedUri().ToString()));
    }

    private static Uri ToMaskedUri(this Uri source)
    {
        if (string.IsNullOrEmpty(source.UserInfo))
        {
            return source;
        }

        var builder = new UriBuilder(source)
        {
            UserName = "****",
#pragma warning disable S2068 // Credentials should not be hard-coded
            Password = "****"
#pragma warning restore S2068 // Credentials should not be hard-coded
        };

        return builder.Uri;
    }
}