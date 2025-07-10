// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Common.Configuration;

internal static class ConfigurationKeyConverter
{
    private const string DotDelimiterString = ".";
    private const char DotDelimiterChar = '.';
    private const char UnderscoreDelimiterChar = '_';
    private const char EscapeChar = '\\';
    private const string EscapeString = "\\";

    private static readonly Regex ArrayRegex = new(@"\[(?<digits>\d+)\]", RegexOptions.Compiled | RegexOptions.Singleline, TimeSpan.FromSeconds(1));

    public static string AsDotNetConfigurationKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return key;
        }

        IEnumerable<string> split = UniversalHierarchySplit(key);
        var sb = new StringBuilder();

        foreach (string keyPart in split.Select(ConvertArrayKey))
        {
            sb.Append(keyPart);
            sb.Append(ConfigurationPath.KeyDelimiter);
        }

        return sb.ToString(0, sb.Length - 1);
    }

    private static List<string> UniversalHierarchySplit(string source)
    {
        List<string> result = [];

        int segmentStart = 0;

        for (int i = 0; i < source.Length; i++)
        {
            bool readEscapeChar = false;

            if (source[i] == EscapeChar)
            {
                readEscapeChar = true;
                i++;
            }

            if (!readEscapeChar && source[i] == DotDelimiterChar)
            {
                result.Add(UnEscapeString(source[segmentStart..i]));
                segmentStart = i + 1;
            }

            if (!readEscapeChar && source[i] == UnderscoreDelimiterChar && i < source.Length - 1 && source[i + 1] == UnderscoreDelimiterChar)
            {
                result.Add(UnEscapeString(source[segmentStart..i]));
                segmentStart = i + 2;
            }

            if (i == source.Length - 1)
            {
                result.Add(UnEscapeString(source[segmentStart..]));
            }
        }

        return result;

        static string UnEscapeString(string src)
        {
            return src.Replace(EscapeString + DotDelimiterString, DotDelimiterString, StringComparison.Ordinal)
                .Replace(EscapeString + EscapeString, EscapeString, StringComparison.Ordinal);
        }
    }

    private static string ConvertArrayKey(string key)
    {
        return ArrayRegex.Replace(key, ":${digits}");
    }
}
