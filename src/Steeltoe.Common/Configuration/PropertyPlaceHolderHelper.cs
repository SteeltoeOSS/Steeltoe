// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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

        private static string ParseStringValue(string property, IConfiguration config, ISet<string> visitedPlaceHolders, ILogger logger = null)
        {
            if (config == null)
            {
                return property;
            }

            if (string.IsNullOrEmpty(property))
            {
                return property;
            }

            StringBuilder result = new StringBuilder(property);

            int startIndex = property.IndexOf(PREFIX);
            while (startIndex != -1)
            {
                int endIndex = FindEndIndex(result, startIndex);
                if (endIndex != -1)
                {
                    string placeholder = result.Substring(startIndex + PREFIX.Length, endIndex);
                    string originalPlaceholder = placeholder;

                    if (!visitedPlaceHolders.Add(originalPlaceholder))
                    {
                        throw new ArgumentException($"Circular placeholder reference '{originalPlaceholder}' in property definitions");
                    }

                    // Recursive invocation, parsing placeholders contained in the placeholder key.
                    placeholder = ParseStringValue(placeholder, config, visitedPlaceHolders);

                    // Handle array references foo:bar[1]:baz format -> foo:bar:1:baz
                    string lookup = placeholder.Replace('[', ':').Replace("]", string.Empty);

                    // Now obtain the value for the fully resolved key...
                    string propVal = config[lookup];
                    if (propVal == null)
                    {
                        int separatorIndex = placeholder.IndexOf(SEPARATOR);
                        if (separatorIndex != -1)
                        {
                            string actualPlaceholder = placeholder.Substring(0, separatorIndex);
                            string defaultValue = placeholder.Substring(separatorIndex + SEPARATOR.Length);
                            propVal = config[actualPlaceholder];
                            if (propVal == null)
                            {
                                propVal = defaultValue;
                            }
                        }
                    }

                    if (propVal != null)
                    {
                        // Recursive invocation, parsing placeholders contained in these
                        // previously resolved placeholder value.
                        propVal = ParseStringValue(propVal, config, visitedPlaceHolders);
                        result.Replace(startIndex, endIndex + SUFFIX.Length, propVal);
                        logger?.LogDebug("Resolved placeholder '{0}'" + placeholder);
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
            int index = startIndex + PREFIX.Length;
            int withinNestedPlaceholder = 0;
            while (index < property.Length)
            {
                if (SubstringMatch(property, index, SUFFIX))
                {
                    if (withinNestedPlaceholder > 0)
                    {
                        withinNestedPlaceholder--;
                        index = index + SUFFIX.Length;
                    }
                    else
                    {
                        return index;
                    }
                }
                else if (SubstringMatch(property, index, PREFIX))
                {
                    withinNestedPlaceholder++;
                    index = index + PREFIX.Length;
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
            for (int j = 0; j < substring.Length; j++)
            {
                int i = index + j;
                if (i >= str.Length || str[i] != substring[j])
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
