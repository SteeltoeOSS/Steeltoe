// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Steeltoe.Common.Configuration
{
    /// <summary>
    /// Utility class for working with configuration values that have placeholders in them.
    /// A placeholder takes the form of <code> ${some:config:reference?default_if_not_present}></code>
    /// Note: This was "inspired" by the Spring class: PropertyPlaceholderHelper
    /// </summary>
    public static class PropertyPlaceholderHelper
    {
        private const string PREFIX = "${";
        private const string SUFFIX = "}";
        private const string SIMPLE_PREFIX = "{";
        private const string SEPARATOR = "?";

        /// <summary>
        /// Replaces all placeholders of the form <code> ${some:config:reference?default_if_not_present}</code>
        /// with the corresponding value from the supplied <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="property">the string containing one or more placeholders</param>
        /// <param name="config">the configuration used for finding replace values.</param>
        /// <param name="logger">optional logger</param>
        /// <returns>the supplied value with the placeholders replaced inline</returns>
        public static string ResolvePlaceholders(string property, IConfiguration config, ILogger logger = null)
        {
            return ParseStringValue(property, config, new HashSet<string>(), logger);
        }

        /// <summary>
        /// Finds all placeholders of the form <code> ${some:config:reference?default_if_not_present}</code>,
        /// resolves them from other values in the configuration, returns a new list to add to your configuration.
        /// </summary>
        /// <param name="config">The configuration to use as both source and target for placeholder resolution.</param>
        /// <param name="logger">Optional logger</param>
        /// <param name="useEmptyStringIfNotFound">Replace the placeholder with an empty string, so the application does not see it</param>
        /// <returns>A list of keys with resolved values. Add to your <see cref="ConfigurationBuilder"/> with method 'AddInMemoryCollection'</returns>
        public static IEnumerable<KeyValuePair<string, string>> GetResolvedConfigurationPlaceholders(IConfiguration config, ILogger logger = null, bool useEmptyStringIfNotFound = true)
        {
            // setup a holding tank for resolved values
            var resolvedValues = new Dictionary<string, string>();
            var visitedPlaceholders = new HashSet<string>();

            // iterate all config entries where the value isn't null and contains both the prefix and suffix that identify placeholders
            foreach (var entry in config.AsEnumerable().Where(e => e.Value != null && e.Value.Contains(PREFIX) && e.Value.Contains(SUFFIX)))
            {
                logger?.LogTrace("Found a property placeholder '{placeholder}' to resolve for key '{key}", entry.Value, entry.Key);
                resolvedValues.Add(entry.Key, ParseStringValue(entry.Value, config, visitedPlaceholders, logger, useEmptyStringIfNotFound));
            }

            return resolvedValues;
        }

        private static string ParseStringValue(string property, IConfiguration config, ISet<string> visitedPlaceHolders, ILogger logger = null, bool useEmptyStringIfNotFound = false)
        {
            if (config == null)
            {
                return property;
            }

            if (string.IsNullOrEmpty(property))
            {
                return property;
            }

            var startIndex = property.IndexOf(PREFIX);
            if (startIndex == -1)
            {
                return property;
            }

            var result = new StringBuilder(property);

            while (startIndex != -1)
            {
                var endIndex = FindEndIndex(result, startIndex);
                if (endIndex != -1)
                {
                    var placeholder = result.Substring(startIndex + PREFIX.Length, endIndex);

                    var originalPlaceholder = placeholder;

                    if (!visitedPlaceHolders.Add(originalPlaceholder))
                    {
                        throw new ArgumentException($"Circular placeholder reference '{originalPlaceholder}' in property definitions");
                    }

                    // Recursive invocation, parsing placeholders contained in the placeholder key.
                    placeholder = ParseStringValue(placeholder, config, visitedPlaceHolders);

                    // Handle array references foo:bar[1]:baz format -> foo:bar:1:baz
                    var lookup = placeholder.Replace('[', ':').Replace("]", string.Empty);

                    // Now obtain the value for the fully resolved key...
                    var propVal = config[lookup];
                    if (propVal == null)
                    {
                        var separatorIndex = placeholder.IndexOf(SEPARATOR);
                        if (separatorIndex != -1)
                        {
                            var actualPlaceholder = placeholder.Substring(0, separatorIndex);
                            var defaultValue = placeholder.Substring(separatorIndex + SEPARATOR.Length);
                            propVal = config[actualPlaceholder] ?? defaultValue;
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
                        propVal = config[lookup];
                    }

                    if (propVal != null)
                    {
                        // Recursive invocation, parsing placeholders contained in these
                        // previously resolved placeholder value.
                        propVal = ParseStringValue(propVal, config, visitedPlaceHolders);
                        result.Replace(startIndex, endIndex + SUFFIX.Length, propVal);
                        logger?.LogDebug("Resolved placeholder '{placeholder}'", placeholder);
                        startIndex = result.IndexOf(PREFIX, startIndex + propVal.Length);
                    }
                    else
                    {
                        // Proceed with unprocessed value.
                        startIndex = result.IndexOf(PREFIX, endIndex + PREFIX.Length);
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
            var index = startIndex + PREFIX.Length;
            var withinNestedPlaceholder = 0;
            while (index < property.Length)
            {
                if (SubstringMatch(property, index, SUFFIX))
                {
                    if (withinNestedPlaceholder > 0)
                    {
                        withinNestedPlaceholder--;
                        index += SUFFIX.Length;
                    }
                    else
                    {
                        return index;
                    }
                }
                else if (SubstringMatch(property, index, SIMPLE_PREFIX))
                {
                    withinNestedPlaceholder++;
                    index += PREFIX.Length;
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

            for (var i = 0; i < substring.Length; i++)
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
}
