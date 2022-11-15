// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;

namespace Steeltoe.Management.Endpoint.Env;

public class Sanitizer
{
    private readonly string[] _regexParts =
    {
        "*",
        "$",
        "^",
        "+"
    };

    private readonly List<Regex> _matchers = new();

    public Sanitizer(string[] keysToSanitize)
    {
        foreach (string key in keysToSanitize)
        {
            string regexPattern = IsRegex(key) ? key : $".*{key}$";

            _matchers.Add(new Regex(regexPattern, RegexOptions.IgnoreCase));
        }
    }

    public KeyValuePair<string, string> Sanitize(KeyValuePair<string, string> kvp)
    {
        if (kvp.Value != null && _matchers.Any(m => m.IsMatch(kvp.Key)))
        {
            return new KeyValuePair<string, string>(kvp.Key, "******");
        }

        return kvp;
    }

    private bool IsRegex(string value)
    {
        foreach (string part in _regexParts)
        {
            if (value.Contains(part, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
