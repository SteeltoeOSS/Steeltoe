// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Steeltoe.Management.Endpoint.Env;

public class Sanitizer
{
    private const string PasswordMaskReplacement = "${leading}${whitespaceBeforeKey}${key}${equals}******";

    private static readonly Regex UriUserInfoRegex = new ("://([^:]*?):[^@]+?@", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    private static readonly Regex PasswordPairRegex = new (
        @"(?<leading>^|;)(?<whitespaceBeforeKey>\s*)(?<key>password|pwd)(?<equals>\s*=\s*)(?<value>[^;]+)",
        RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    private readonly string[] _regex_parts = new string[] { "*", "$", "^", "+" };
    private readonly List<Regex> _matchers = new ();

    public Sanitizer(string[] keysToSanitize)
    {
        foreach (var key in keysToSanitize)
        {
            var regexPattern = IsRegex(key) ? key : $".*{key}$";

            _matchers.Add(new Regex(regexPattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)));
        }
    }

    public KeyValuePair<string, string> Sanitize(KeyValuePair<string, string> kvp)
    {
        if (_matchers.Any(m => m.IsMatch(kvp.Key)))
        {
            return new KeyValuePair<string, string>(kvp.Key, "******");
        }

        if (kvp.Value != null)
        {
            var maskedValue = PasswordPairRegex.Replace(kvp.Value, PasswordMaskReplacement);
            maskedValue = UriUserInfoRegex.Replace(maskedValue, "://$1:******@");
            return new KeyValuePair<string, string>(kvp.Key, maskedValue);
        }

        return kvp;
    }

    private bool IsRegex(string value)
    {
        foreach (var part in _regex_parts)
        {
            if (value.Contains(part))
            {
                return true;
            }
        }

        return false;
    }
}