// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Common.Configuration;

/// <summary>
/// Utility class for working with configuration values that have placeholders in them. A placeholder takes the form of:
/// <code> ${some:config:reference?default_if_not_present}></code> Note: This was "inspired" by the Spring class: PropertyPlaceholderHelper.
/// </summary>
public static class PropertyPlaceholderHelper
{
    private const string Prefix = "${";
    private const string Suffix = "}";
    private const string SimplePrefix = "{";
    private const string Separator = "?";

    /// <summary>
    /// Replaces all placeholders of the form: <code> ${some:config:reference?default_if_not_present}</code> with the corresponding value from the supplied
    /// <see cref="IConfiguration" />.
    /// </summary>
    /// <param name="property">
    /// the string containing one or more placeholders.
    /// </param>
    /// <param name="configuration">
    /// the configuration used for finding replace values.
    /// </param>
    /// <param name="logger">
    /// optional logger.
    /// </param>
    /// <returns>
    /// the supplied value with the placeholders replaced inline.
    /// </returns>
    public static string ResolvePlaceholders(string property, IConfiguration configuration, ILogger logger = null)
    {
        return ParseStringValue(property, configuration, new HashSet<string>(), logger);
    }

    /// <summary>
    /// Finds all placeholders of the form: <code> ${some:config:reference?default_if_not_present}</code>, resolves them from other values in the
    /// configuration, returns a new list to add to your configuration.
    /// </summary>
    /// <param name="configuration">
    /// The configuration to use as both source and target for placeholder resolution.
    /// </param>
    /// <param name="logger">
    /// Optional logger.
    /// </param>
    /// <param name="useEmptyStringIfNotFound">
    /// Replace the placeholder with an empty string, so the application does not see it.
    /// </param>
    /// <returns>
    /// A list of keys with resolved values. Add to your <see cref="ConfigurationBuilder" /> with method 'AddInMemoryCollection'.
    /// </returns>
    public static IEnumerable<KeyValuePair<string, string>> GetResolvedConfigurationPlaceholders(IConfiguration configuration, ILogger logger = null,
        bool useEmptyStringIfNotFound = true)
    {
        // setup a holding tank for resolved values
        var resolvedValues = new Dictionary<string, string>();
        var visitedPlaceholders = new HashSet<string>();

        // iterate all configuration entries where the value isn't null and contains both the prefix and suffix that identify placeholders
        foreach (KeyValuePair<string, string> entry in configuration.AsEnumerable()
            .Where(e => e.Value != null && e.Value.Contains(Prefix) && e.Value.Contains(Suffix)))
        {
            logger?.LogTrace("Found a property placeholder '{placeholder}' to resolve for key '{key}", entry.Value, entry.Key);
            resolvedValues.Add(entry.Key, ParseStringValue(entry.Value, configuration, visitedPlaceholders, logger, useEmptyStringIfNotFound));
        }

        return resolvedValues;
    }

    private static string ParseStringValue(string property, IConfiguration configuration, ISet<string> visitedPlaceHolders, ILogger logger = null,
        bool useEmptyStringIfNotFound = false)
    {
        if (configuration == null)
        {
            return property;
        }

        if (string.IsNullOrEmpty(property))
        {
            return property;
        }

        int startIndex = property.IndexOf(Prefix);

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
                string placeholder = result.Substring(startIndex + Prefix.Length, endIndex);

                string originalPlaceholder = placeholder;

                if (!visitedPlaceHolders.Add(originalPlaceholder))
                {
                    throw new InvalidOperationException($"Found circular placeholder reference '{originalPlaceholder}' in property definitions.");
                }

                // Recursive invocation, parsing placeholders contained in the placeholder key.
                placeholder = ParseStringValue(placeholder, configuration, visitedPlaceHolders);

                // Handle array references foo:bar[1]:baz format -> foo:bar:1:baz
                string lookup = placeholder.Replace('[', ':').Replace("]", string.Empty);

                // Now obtain the value for the fully resolved key...
                string propVal = configuration[lookup];

                if (propVal == null)
                {
                    int separatorIndex = placeholder.IndexOf(Separator);

                    if (separatorIndex != -1)
                    {
                        string actualPlaceholder = placeholder.Substring(0, separatorIndex);
                        string defaultValue = placeholder.Substring(separatorIndex + Separator.Length);
                        propVal = configuration[actualPlaceholder] ?? defaultValue;
                    }
                    else if (useEmptyStringIfNotFound)
                    {
                        propVal = string.Empty;
                    }
                }

                // Attempt to resolve as a spring-compatible placeholder
                if (propVal == null)
                {
                    // Replace Spring delimiters ('.') with MS-friendly delimiters (':') so Spring placeholders can also be resolved
                    lookup = placeholder.Replace(".", ":");
                    propVal = configuration[lookup];
                }

                if (propVal != null)
                {
                    // Recursive invocation, parsing placeholders contained in these
                    // previously resolved placeholder value.
                    propVal = ParseStringValue(propVal, configuration, visitedPlaceHolders);
                    result.Replace(startIndex, endIndex + Suffix.Length, propVal);
                    logger?.LogDebug("Resolved placeholder '{placeholder}'", placeholder);
                    startIndex = result.IndexOf(Prefix, startIndex + propVal.Length);
                }
                else
                {
                    // Proceed with unprocessed value.
                    startIndex = result.IndexOf(Prefix, endIndex + Prefix.Length);
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

    private static int FindEndIndex(StringBuilder property, int startIndex)
    {
        int index = startIndex + Prefix.Length;
        int withinNestedPlaceholder = 0;

        while (index < property.Length)
        {
            if (SubstringMatch(property, index, Suffix))
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
            else if (SubstringMatch(property, index, SimplePrefix))
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

    private static bool SubstringMatch(StringBuilder str, int index, string substring)
    {
        if (index + substring.Length > str.Length)
        {
            return false;
        }

        for (int i = 0; i < substring.Length; i++)
        {
            if (str[index + i] != substring[i])
            {
                return false;
            }
        }

        return true;
    }

    private static void Replace(this StringBuilder builder, int start, int end, string str)
    {
        builder.Remove(start, end - start);
        builder.Insert(start, str);
    }

    private static int IndexOf(this StringBuilder builder, string str, int start)
    {
        if (start >= builder.Length)
        {
            return -1;
        }

        return builder.ToString().IndexOf(str, start);
    }

    private static string Substring(this StringBuilder builder, int start, int end)
    {
        return builder.ToString().Substring(start, end - start);
    }
}
