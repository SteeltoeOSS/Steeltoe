// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Actuators.Environment;

internal sealed class Sanitizer
{
    private readonly char[] _regexCharacters =
    {
        '*',
        '$',
        '^',
        '+'
    };

    private readonly List<Regex> _matchers = new();

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

    public string? Sanitize(string key, string? value)
    {
        if (value != null && _matchers.Exists(regex => regex.IsMatch(key)))
        {
            return "******";
        }

        return value;
    }

    private bool IsRegex(string value)
    {
        return value.IndexOfAny(_regexCharacters) != -1;
    }
}
