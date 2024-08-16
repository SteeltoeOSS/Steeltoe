// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Common.Configuration;

/// <summary>
/// Utility class for working with configuration values that have placeholders in them. A placeholder takes the form of:
/// <code><![CDATA[
/// ${some:config:reference?default_if_not_present}
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
    /// ${some:config:reference?default_if_not_present}
    /// ]]></code> with the corresponding value from
    /// the supplied <see cref="IConfiguration" />.
    /// </summary>
    /// <param name="property">
    /// The string containing one or more placeholders.
    /// </param>
    /// <param name="configuration">
    /// The configuration used for finding replacement values.
    /// </param>
    /// <returns>
    /// The supplied value, with the placeholders replaced inline.
    /// </returns>
    public string? ResolvePlaceholders(string? property, IConfiguration? configuration)
    {
        return ParseStringValue(property, configuration, false, new HashSet<string>());
    }

    /// <summary>
    /// Finds all placeholders of the form: <code><![CDATA[
    /// ${some:config:reference?default_if_not_present}
    /// ]]></code>, resolves them from other values in
    /// the configuration, and returns a new dictionary to add to your configuration.
    /// </summary>
    /// <param name="configuration">
    /// The configuration to use as both source and target for placeholder resolution.
    /// </param>
    /// <returns>
    /// A list of keys with resolved values. Add them to your <see cref="ConfigurationBuilder" /> with method 'AddInMemoryCollection'.
    /// </returns>
    public IDictionary<string, string?> GetResolvedConfigurationPlaceholders(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // setup a holding tank for resolved values
        var resolvedValues = new Dictionary<string, string?>();
        var visitedPlaceholders = new HashSet<string>();

        // iterate all configuration entries where the value isn't null and contains both the prefix and suffix that identify placeholders
        foreach ((string key, string? value) in configuration.AsEnumerable().Where(pair =>
            pair.Value != null && pair.Value.Contains(Prefix, StringComparison.Ordinal) && pair.Value.Contains(Suffix, StringComparison.Ordinal)))
        {
            _logger.LogTrace("Found a property placeholder '{Placeholder}' to resolve for key '{Key}", value, key);
            resolvedValues.Add(key, ParseStringValue(value, configuration, true, visitedPlaceholders));
        }

        return resolvedValues;
    }

    [return: NotNullIfNotNull(nameof(property))]
    private string? ParseStringValue(string? property, IConfiguration? configuration, bool useEmptyStringIfNotFound, ISet<string> visitedPlaceHolders)
    {
        if (configuration == null)
        {
            return property;
        }

        if (string.IsNullOrEmpty(property))
        {
            return property;
        }

        int startIndex = property.IndexOf(Prefix, StringComparison.Ordinal);

        if (startIndex == -1)
        {
            return property;
        }

        var result = new StringBuilder(property);

        while (startIndex != -1)
        {
            int endIndex = FindEndIndex(result, startIndex);

            if (endIndex != -1)
            {
                string placeholder = Substring(result, startIndex + Prefix.Length, endIndex);

                string originalPlaceholder = placeholder;

                if (!visitedPlaceHolders.Add(originalPlaceholder))
                {
                    throw new InvalidOperationException($"Found circular placeholder reference '{originalPlaceholder}' in property definitions.");
                }

                // Recursive invocation, parsing placeholders contained in the placeholder key.
                placeholder = ParseStringValue(placeholder, configuration, useEmptyStringIfNotFound, visitedPlaceHolders);

                // Handle array references foo:bar[1]:baz format -> foo:bar:1:baz
                string lookup = placeholder.Replace('[', ':').Replace("]", string.Empty, StringComparison.Ordinal);

                // Now obtain the value for the fully resolved key...
                string? propertyValue = configuration[lookup];

                if (propertyValue == null)
                {
                    int separatorIndex = placeholder.IndexOf(Separator, StringComparison.Ordinal);

                    if (separatorIndex != -1)
                    {
                        string actualPlaceholder = placeholder.Substring(0, separatorIndex);
                        string defaultValue = placeholder.Substring(separatorIndex + Separator.Length);
                        propertyValue = configuration[actualPlaceholder] ?? defaultValue;
                    }
                    else if (useEmptyStringIfNotFound)
                    {
                        propertyValue = string.Empty;
                    }
                }

                // Attempt to resolve as a spring-compatible placeholder
                if (propertyValue == null)
                {
                    // Replace Spring delimiters ('.') with MS-friendly delimiters (':') so Spring placeholders can also be resolved
                    lookup = placeholder.Replace('.', ':');
                    propertyValue = configuration[lookup];
                }

                if (propertyValue != null)
                {
                    // Recursive invocation, parsing placeholders contained in these previously resolved placeholder value.
                    propertyValue = ParseStringValue(propertyValue, configuration, useEmptyStringIfNotFound, visitedPlaceHolders);
                    Replace(result, startIndex, endIndex + Suffix.Length, propertyValue);
                    _logger.LogDebug("Resolved placeholder '{Placeholder}'", placeholder);
                    startIndex = IndexOf(result, Prefix, startIndex + propertyValue.Length);
                }
                else
                {
                    // Proceed with unprocessed value.
                    startIndex = IndexOf(result, Prefix, endIndex + Prefix.Length);
                }

                visitedPlaceHolders.Remove(originalPlaceholder);
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
        return builder.ToString().Substring(start, end - start);
    }
}
