// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Actuators.Environment;

internal sealed partial class Sanitizer
{
    private readonly char[] _regexCharacters =
    [
        '*',
        '$',
        '^',
        '+'
    ];

    private readonly List<Regex> _matchers = [];

    public Sanitizer(ICollection<string> keysToSanitize)
    {
        ArgumentNullException.ThrowIfNull(keysToSanitize);
        ArgumentGuard.ElementsNotNullOrEmpty(keysToSanitize);

        foreach (string key in keysToSanitize)
        {
            string regexPattern = IsRegex(key) ? key : $".*{key}$";
            _matchers.Add(new Regex(regexPattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)));
        }
    }

    [GeneratedRegex("://([^:]*?):[^@]+?@", RegexOptions.None, 1000)]
    private static partial Regex UriUserInfoRegex();

    [GeneratedRegex(@"(?<leading>^|;)(?<whitespaceBeforeKey>\s*)(?<key>password|pwd)(?<equals>\s*=\s*)(?<value>[^;]+)",
        RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 1000)]
    private static partial Regex PasswordPairRegex();

    public string? Sanitize(string key, string? value)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_matchers.Exists(regex => regex.IsMatch(key)))
        {
            return "******";
        }

        if (value == null)
        {
            return null;
        }

        string maskedValue = PasswordPairRegex().Replace(value, "${leading}${whitespaceBeforeKey}${key}${equals}******");
        maskedValue = UriUserInfoRegex().Replace(maskedValue, "://$1:******@");

        return maskedValue;
    }

    private bool IsRegex(string value)
    {
        return value.IndexOfAny(_regexCharacters) != -1;
    }
}
