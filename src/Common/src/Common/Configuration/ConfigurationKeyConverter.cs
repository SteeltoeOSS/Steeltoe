// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Common.Configuration;

/// <summary>
/// Converts Spring-style configuration keys to .NET configuration keys.
/// </summary>
/// <remarks>
/// <para>
/// Key segments are separated by a dot, a colon or a double underscore. A dot or square bracket can be escaped with a backslash.
/// </para>
/// <para>
/// Like in Spring, a segment can be surrounded by square brackets to indicate an array index or a dictionary key. The text between the brackets is used
/// as-is, so it may contain dots. For example: <c>a[0].b</c> converts to <c>a:0:b</c>, <c>a[b.c]</c> converts to <c>a:b.c</c> and <c>[a.b].c</c>
/// converts to <c>a.b:c</c>.
/// </para>
/// </remarks>
internal static class ConfigurationKeyConverter
{
    private const char DotDelimiterChar = '.';
    private const char ColonDelimiterChar = ':';
    private const char UnderscoreDelimiterChar = '_';
    private const char EscapeChar = '\\';
    private const char OpenBracketChar = '[';
    private const char CloseBracketChar = ']';

    public static string AsDotNetConfigurationKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return key;
        }

        List<string> segments = SplitIntoSegments(key);
        return string.Join(ConfigurationPath.KeyDelimiter, segments);
    }

    private static List<string> SplitIntoSegments(string source)
    {
        List<string> segments = [];
        var segmentBuilder = new StringBuilder();

        // Indicates whether the segment being built must be added at the end, even when empty (for example, after a trailing delimiter).
        bool isSegmentPending = true;

        for (int index = 0; index < source.Length; index++)
        {
            char ch = source[index];

            if (ch == EscapeChar && index < source.Length - 1)
            {
                index++;
                AppendEscaped(segmentBuilder, source[index]);
                isSegmentPending = true;
            }
            else if (IsDelimiterAt(source, index, out int delimiterLength))
            {
                // A closing bracket already ended the previous segment, so a delimiter directly after it is optional, for example: a[0].b or a[0]b.
                if (isSegmentPending)
                {
                    AddSegment(segments, segmentBuilder);
                }

                index += delimiterLength - 1;
                isSegmentPending = true;
            }
            else if (ch == OpenBracketChar && TryFindClosingBracket(source, index, out int closeIndex))
            {
                // A bracket starts a new segment, so it must not produce an empty segment, for example: [a.b], a[0] or a.[b].
                if (segmentBuilder.Length > 0)
                {
                    AddSegment(segments, segmentBuilder);
                }

                segments.Add(source[(index + 1)..closeIndex]);
                index = closeIndex;
                isSegmentPending = false;
            }
            else
            {
                segmentBuilder.Append(ch);
                isSegmentPending = true;
            }
        }

        if (isSegmentPending)
        {
            AddSegment(segments, segmentBuilder);
        }

        return segments;
    }

    private static void AppendEscaped(StringBuilder segmentBuilder, char escapedChar)
    {
        // Only characters with a special meaning can be escaped. For all others, the escape character is preserved.
        if (escapedChar is not (DotDelimiterChar or EscapeChar or OpenBracketChar or CloseBracketChar))
        {
            segmentBuilder.Append(EscapeChar);
        }

        segmentBuilder.Append(escapedChar);
    }

    private static bool IsDelimiterAt(string source, int index, out int delimiterLength)
    {
        if (source[index] is DotDelimiterChar or ColonDelimiterChar)
        {
            delimiterLength = 1;
            return true;
        }

        if (source[index] == UnderscoreDelimiterChar && index < source.Length - 1 && source[index + 1] == UnderscoreDelimiterChar)
        {
            delimiterLength = 2;
            return true;
        }

        delimiterLength = 0;
        return false;
    }

    private static void AddSegment(List<string> segments, StringBuilder segmentBuilder)
    {
        segments.Add(segmentBuilder.ToString());
        segmentBuilder.Clear();
    }

    private static bool TryFindClosingBracket(string source, int openIndex, out int closeIndex)
    {
        // Brackets can be nested, for example: a[b[0]] converts to a:b[0]. Empty or unbalanced brackets are preserved as literal text.
        int depth = 0;

        for (int index = openIndex; index < source.Length; index++)
        {
            if (source[index] == OpenBracketChar)
            {
                depth++;
            }
            else if (source[index] == CloseBracketChar)
            {
                depth--;

                if (depth == 0)
                {
                    closeIndex = index;
                    return closeIndex > openIndex + 1;
                }
            }
        }

        closeIndex = -1;
        return false;
    }
}
