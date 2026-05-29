// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Extensions;

/// <summary>
/// Represents a <see cref="Uri" /> whose username and password are masked.
/// </summary>
internal readonly record struct MaskedUri
{
    private readonly Uri? _value;

    public MaskedUri(Uri? value)
    {
        _value = value;
    }

    public override string ToString()
    {
        return _value == null ? string.Empty : ToMaskedString(_value);
    }

    private static string ToMaskedString(Uri source)
    {
        string uris = source.ToString();

        if (uris.Contains(','))
        {
            return string.Join(',',
                uris.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(uri => Mask(new Uri(uri)).ToString()));
        }

        return Mask(source).ToString();
    }

    private static Uri Mask(Uri source)
    {
        if (string.IsNullOrEmpty(source.UserInfo))
        {
            return source;
        }

        var builder = new UriBuilder(source)
        {
            UserName = "****",
#pragma warning disable S2068 // Hard-coded credentials are security-sensitive
            Password = "****"
#pragma warning restore S2068 // Hard-coded credentials are security-sensitive
        };

        return builder.Uri;
    }

    public static implicit operator MaskedUri(Uri? uri)
    {
        return new MaskedUri(uri);
    }
}
