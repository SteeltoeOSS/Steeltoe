// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Steeltoe.Common.Configuration;

internal static class ConfigurationValuesHelper
{
    public static string? GetSetting(string key, IConfiguration primary, IConfiguration secondary, IConfiguration? resolve, string? defaultValue)
    {
        // First check for key in primary
        string? setting = GetString(key, primary, resolve, null);

        if (!string.IsNullOrEmpty(setting))
        {
            return setting;
        }

        // Next check for key in secondary
        setting = GetString(key, secondary, resolve, null);

        if (!string.IsNullOrEmpty(setting))
        {
            return setting;
        }

        return defaultValue;
    }

    /// <summary>
    /// Gets a setting from configuration by searching the given section prefix keys in order. Returns the first match.
    /// </summary>
    /// <param name="key">
    /// The key of the element to return.
    /// </param>
    /// <param name="configuration">
    /// The <see cref="IConfiguration" /> to search through.
    /// </param>
    /// <param name="defaultValue">
    /// The default value to return if no configuration is found.
    /// </param>
    /// <param name="sectionPrefixes">
    /// The prefixes to search for in given order.
    /// </param>
    /// <returns>
    /// The value from configuration, or the default value if not found.
    /// </returns>
    public static string? GetSetting(string key, IConfiguration configuration, string? defaultValue, params string[] sectionPrefixes)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(sectionPrefixes);
        ArgumentGuard.ElementsNotNullOrEmpty(sectionPrefixes);

        foreach (string prefix in sectionPrefixes)
        {
            IConfigurationSection section = configuration.GetSection(prefix);
            string? value = section.GetValue<string>(key);

            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// Gets a setting from configuration by searching the given keys in order. Returns the first match.
    /// </summary>
    /// <param name="configuration">
    /// The <see cref="IConfiguration" /> to search through.
    /// </param>
    /// <param name="defaultValue">
    /// The default value to return if no configuration is found.
    /// </param>
    /// <param name="configurationKeys">
    /// The fully-qualified keys to search for in given order.
    /// </param>
    /// <returns>
    /// The value from configuration, or the default value if not found.
    /// </returns>
    public static string? GetPreferredSetting(IConfiguration configuration, string? defaultValue, params string?[] configurationKeys)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configurationKeys);

        foreach (string key in configurationKeys.Where(key => !string.IsNullOrEmpty(key)).Cast<string>())
        {
            string? value = configuration.GetValue<string>(key);

            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }

        return defaultValue;
    }

    public static int GetInt32(string key, IConfiguration configuration, IConfiguration? resolve, int defaultValue)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(configuration);

        string? value = GetString(key, configuration, resolve, null);

        if (!string.IsNullOrEmpty(value) && int.TryParse(value, CultureInfo.InvariantCulture, out int result))
        {
            return result;
        }

        return defaultValue;
    }

    public static double GetDouble(string key, IConfiguration configuration, IConfiguration? resolve, double defaultValue)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(configuration);

        string? value = GetString(key, configuration, resolve, null);

        if (!string.IsNullOrEmpty(value) &&
            double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double result))
        {
            return result;
        }

        return defaultValue;
    }

    public static bool GetBoolean(string key, IConfiguration configuration, IConfiguration? resolve, bool defaultValue)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(configuration);

        string? value = GetString(key, configuration, resolve, null);

        if (!string.IsNullOrEmpty(value) && bool.TryParse(value, out bool result))
        {
            return result;
        }

        return defaultValue;
    }

    public static string? GetString(string key, IConfiguration configuration, IConfiguration? resolve, string? defaultValue)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(configuration);

        string? value = configuration[key];

        if (!string.IsNullOrEmpty(value))
        {
            return PropertyPlaceholderHelper.ResolvePlaceholders(value, resolve, NullLogger.Instance);
        }

        return defaultValue;
    }
}
