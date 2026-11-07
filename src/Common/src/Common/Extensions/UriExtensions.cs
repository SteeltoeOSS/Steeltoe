// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Steeltoe.Common.Extensions;

internal static class UriExtensions
{
    public static bool TryGetUsernamePassword(this Uri uri, [NotNullWhen(true)] out string? username, [NotNullWhen(true)] out string? password)
    {
        ArgumentNullException.ThrowIfNull(uri);

        string userInfo = uri.GetComponents(UriComponents.UserInfo, UriFormat.UriEscaped);

        string[] parts = userInfo.Split(':', 2);

        if (parts.Length == 2)
        {
            username = Uri.UnescapeDataString(parts[0]);
            password = Uri.UnescapeDataString(parts[1]);
            return true;
        }

        username = null;
        password = null;
        return false;
    }
}
