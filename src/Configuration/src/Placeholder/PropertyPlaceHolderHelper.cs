// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Configuration.Placeholder;

/// <summary>
/// Utility class for working with configuration values that have placeholders in them. A placeholder takes the form of:
/// <code><![CDATA[
/// ${path:to:key?default_if_not_present}
/// ]]></code>
/// <para>
/// Note: This was "inspired" by the Spring class
/// <c>
/// PropertyPlaceholderHelper
/// </c>
/// .
/// </para>
/// </summary>
internal sealed class PropertyPlaceholderHelper
{
    private const string Prefix = "${";
    private const string Suffix = "}";
    private const string SimplePrefix = "{";
    private const string Separator = "?";
    private readonly ILogger<PropertyPlaceholderHelper> _logger;

    public PropertyPlaceholderHelper(ILogger<PropertyPlaceholderHelper> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    /// <summary>
    /// Replaces all placeholders of the form: <code><![CDATA[
    /// ${path:to:key?default_if_not_present}
    /// ]]></code> with the corresponding value from the
    /// supplied <see cref="IConfiguration" />.
    /// </summary>
    /// <param name="text">
    /// A string that can contain a mix of literal text and (recursive) placeholder references.
    /// </param>
    /// <param name="configuration">
    /// The configuration used for finding replacement values.
    /// </param>
    /// <returns>
    /// The incoming text, with all placeholders replaced inline. Unknown placeholders remain as-is.
    /// </returns>
    public string? ResolvePlaceholders(string? text, IConfiguration? configuration)
    {
        return ParseStringValue(text, configuration, new HashSet<string>());
    }

    [return: NotNullIfNotNull(nameof(text))]
    private string? ParseStringValue(string? text, IConfiguration? configuration, ISet<string> visitedPlaceholders)
    {
        if (configuration == null || string.IsNullOrEmpty(text))
        {
            return text;
        }

        int startIndex = text.IndexOf(Prefix, StringComparison.Ordinal);

        if (startIndex == -1)
        {
            return text;
        }

        var result = new StringBuilder(text);

        while (startIndex != -1)
        {
            int endIndex = FindEndIndex(result, startIndex);

            if (endIndex != -1)
            {
                string outerPlaceholder = Substring(result, startIndex + Prefix.Length, endIndex);

                if (!visitedPlaceholders.Add(outerPlaceholder))
                {
                    throw new InvalidOperationException($"Found circular placeholder reference '{outerPlaceholder}' in configuration.");
                }

                // Recursive invocation, parsing placeholders contained in the placeholder key.
                string innerPlaceholder = ParseStringValue(outerPlaceholder, configuration, visitedPlaceholders);
                PlaceholderExpression expression = PlaceholderExpression.Parse(innerPlaceholder);

                // Now obtain the value for the fully resolved key...
                string? propertyValue = configuration[expression.Key];

                // Attempt to resolve as a Spring-compatible placeholder.
                if (propertyValue == null)
                {
                    // Replace Spring delimiters ('.') with dotnet-friendly delimiters (':') so Spring placeholders can also be resolved.
                    string springKey = expression.Key.Replace('.', ':');
                    propertyValue = configuration[springKey];
                }

                propertyValue ??= expression.DefaultValue;

                if (propertyValue != null)
                {
                    // Recursive invocation, parsing placeholders contained in this previously resolved placeholder value.
                    propertyValue = ParseStringValue(propertyValue, configuration, visitedPlaceholders);
                    Replace(result, startIndex, endIndex + Suffix.Length, propertyValue);

                    _logger.LogDebug("Resolved placeholder '{Placeholder}' to '{Value}'", innerPlaceholder, propertyValue);
                    startIndex = IndexOf(result, Prefix, startIndex + propertyValue.Length);
                }
                else
                {
                    // Proceed with unprocessed value.
                    startIndex = IndexOf(result, Prefix, endIndex + Prefix.Length);
                }

                visitedPlaceholders.Remove(outerPlaceholder);
            }
            else
            {
                startIndex = -1;
            }
        }

        return result.ToString();
    }

    private static int FindEndIndex(StringBuilder builder, int startIndex)
    {
        int index = startIndex + Prefix.Length;
        int withinNestedPlaceholder = 0;

        while (index < builder.Length)
        {
            if (SubstringMatch(builder, index, Suffix))
            {
                if (withinNestedPlaceholder > 0)
                {
                    withinNestedPlaceholder--;
                    index += Suffix.Length;
                }
                else
                {
                    return index;
                }
            }
            else if (SubstringMatch(builder, index, SimplePrefix))
            {
                withinNestedPlaceholder++;
                index += Prefix.Length;
            }
            else
            {
                index++;
            }
        }

        return -1;
    }

    private static bool SubstringMatch(StringBuilder builder, int index, string substring)
    {
        if (index + substring.Length > builder.Length)
        {
            return false;
        }

        for (int i = 0; i < substring.Length; i++)
        {
            if (builder[index + i] != substring[i])
            {
                return false;
            }
        }

        return true;
    }

    private static void Replace(StringBuilder builder, int start, int end, string str)
    {
        builder.Remove(start, end - start);
        builder.Insert(start, str);
    }

    private static int IndexOf(StringBuilder builder, string str, int start)
    {
        if (start >= builder.Length)
        {
            return -1;
        }

        return builder.ToString().IndexOf(str, start, StringComparison.Ordinal);
    }

    private static string Substring(StringBuilder builder, int start, int end)
    {
        return builder.ToString()[start..end];
    }

    private readonly struct PlaceholderExpression(string key, string? defaultValue) : IEquatable<PlaceholderExpression>
    {
        public string Key { get; } = key;
        public string? DefaultValue { get; } = defaultValue;

        public static PlaceholderExpression Parse(string text)
        {
            ArgumentNullException.ThrowIfNull(text);

            // Convert array references, for example: foo:bar[1]:baz -> foo:bar:1:baz
            string normalizedText = text.Replace('[', ':').Replace("]", string.Empty, StringComparison.Ordinal);

            // Split into key and optional default value, for example: path:to:key?default -> path:to:key and default
            string[] parts = normalizedText.Split(Separator, 2);

            if (parts.Length == 2)
            {
                return new PlaceholderExpression(parts[0], parts[1]);
            }

            return new PlaceholderExpression(parts[0], null);
        }

        public bool Equals(PlaceholderExpression other)
        {
            return Key == other.Key && DefaultValue == other.DefaultValue;
        }

        public override bool Equals(object? obj)
        {
            return obj is PlaceholderExpression other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Key, DefaultValue);
        }
    }
}
