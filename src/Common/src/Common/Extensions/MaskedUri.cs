// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Specialized;
using System.Web;
#if NET9_0_OR_GREATER
using System.Buffers;
#endif

namespace Steeltoe.Common.Extensions;

/// <summary>
/// Represents a <see cref="Uri" /> whose username, password and sensitive query string parameters are masked.
/// </summary>
internal readonly record struct MaskedUri
{
    private static readonly string[] SensitiveQueryStringParameterNameParts =
    [
        "pass",
        "pwd",
        "key",
        "token",
        "bearer",
        "secret",
        "auth",
        "cred",
        "sig",
        "hash",
        "pin",
        "otp",
        "nonce",
        "cert"
    ];

#if NET9_0_OR_GREATER
    private static readonly SearchValues<string> SensitiveQueryStringParameterNameSearchValues =
        // Vectorized, case-insensitive multi-substring search: much faster than looping over SensitiveQueryStringParameterNameParts per parameter name.
        SearchValues.Create(SensitiveQueryStringParameterNameParts, StringComparison.OrdinalIgnoreCase);
#endif

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

    internal static Uri Mask(Uri source)
    {
        bool hasUserInfo = !string.IsNullOrEmpty(source.UserInfo);
        string? maskedQueryString = MaskQueryString(source.Query);

        if (!hasUserInfo && maskedQueryString == null)
        {
            return source;
        }

        var builder = new UriBuilder(source);

        if (hasUserInfo)
        {
            builder.UserName = "****";
#pragma warning disable S2068 // Hard-coded credentials are security-sensitive
            builder.Password = "****";
#pragma warning restore S2068 // Hard-coded credentials are security-sensitive
        }

        if (maskedQueryString != null)
        {
            builder.Query = maskedQueryString;
        }

        return builder.Uri;
    }

    private static string? MaskQueryString(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return null;
        }

        NameValueCollection parameters = HttpUtility.ParseQueryString(query);
        bool hasMaskedAny = false;

        foreach (string? name in parameters.AllKeys)
        {
            if (name != null && IsSensitiveParameterName(name))
            {
                parameters[name] = "****";
                hasMaskedAny = true;
            }
        }

        return hasMaskedAny ? parameters.ToString() : null;
    }

    private static bool IsSensitiveParameterName(string parameterName)
    {
#if NET9_0_OR_GREATER
        return parameterName.AsSpan().ContainsAny(SensitiveQueryStringParameterNameSearchValues);
#else
        foreach (string namePart in SensitiveQueryStringParameterNameParts)
        {
            if (parameterName.Contains(namePart, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
#endif
    }

    public static implicit operator MaskedUri(Uri? uri)
    {
        return new MaskedUri(uri);
    }
}
