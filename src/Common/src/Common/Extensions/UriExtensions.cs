// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Steeltoe.Common.Extensions;

internal static class UriExtensions
{
    public static string ToMaskedString(this Uri source)
    {
        ArgumentGuard.NotNull(source);

        return source.ToMaskedUri().ToString();
    }

    public static Uri ToMaskedUri(this Uri source)
    {
        ArgumentGuard.NotNull(source);

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

    public static bool TryGetUsernamePassword(this Uri uri, [NotNullWhen(true)] out string? username, [NotNullWhen(true)] out string? password)
    {
        ArgumentGuard.NotNull(uri);

        string userInfo = uri.GetComponents(UriComponents.UserInfo, UriFormat.UriEscaped);

        string[] parts = userInfo.Split(':');

        if (parts.Length == 2)
        {
            username = WebUtility.UrlDecode(parts[0]);
            password = WebUtility.UrlDecode(parts[1]);
            return true;
        }

        username = null;
        password = null;
        return false;
    }
}
