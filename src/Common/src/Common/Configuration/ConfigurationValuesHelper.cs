// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Common.Configuration;

public static class ConfigurationValuesHelper
{
    public static string GetSetting(string key, IConfiguration primary, IConfiguration secondary, IConfiguration resolve, string def)
    {
        // First check for key in primary
        string setting = GetString(key, primary, resolve, null);

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

        return def;
    }

    /// <summary>
    /// Get setting from configuration searching the given sectionPrefix keys in order. Returns the first element with key.
    /// </summary>
    /// <param name="key">
    /// The key of the element to return.
    /// </param>
    /// <param name="configuration">
    /// IConfiguration to search through.
    /// </param>
    /// <param name="defaultValue">
    /// The default Value if no configuration is found.
    /// </param>
    /// <param name="sectionPrefixes">
    /// The prefixes to search for in given order.
    /// </param>
    /// <returns>
    /// Configuration value.
    /// </returns>
    public static string GetSetting(string key, IConfiguration configuration, string defaultValue, params string[] sectionPrefixes)
    {
        foreach (string prefix in sectionPrefixes)
        {
            IConfigurationSection section = configuration.GetSection(prefix);
            string result = section.GetValue<string>(key);

            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// Get a setting from configuration by searching the given keys in order. Returns the first match.
    /// </summary>
    /// <param name="configuration">
    /// IConfiguration to search through.
    /// </param>
    /// <param name="defaultValue">
    /// The default Value if no configuration is found.
    /// </param>
    /// <param name="configKeys">
    /// The fully-qualified keys to search for in given order.
    /// </param>
    /// <returns>
    /// Value from configuration or default (if not found).
    /// </returns>
    public static string GetPreferredSetting(IConfiguration configuration, string defaultValue, params string[] configKeys)
    {
        foreach (string key in configKeys.Where(c => !string.IsNullOrEmpty(c)))
        {
            string result = configuration.GetValue<string>(key);

            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }
        }

        return defaultValue;
    }

    public static int GetInt(string key, IConfiguration configuration, IConfiguration resolve, int def)
    {
        string val = GetString(key, configuration, resolve, null);

        if (!string.IsNullOrEmpty(val) && int.TryParse(val, out int result))
        {
            return result;
        }

        return def;
    }

    public static double GetDouble(string key, IConfiguration configuration, IConfiguration resolve, double def)
    {
        string val = GetString(key, configuration, resolve, null);

        if (!string.IsNullOrEmpty(val) && double.TryParse(val, out double result))
        {
            return result;
        }

        return def;
    }

    public static bool GetBoolean(string key, IConfiguration configuration, IConfiguration resolve, bool def)
    {
        string val = GetString(key, configuration, resolve, null);

        if (!string.IsNullOrEmpty(val) && bool.TryParse(val, out bool result))
        {
            return result;
        }

        return def;
    }

    public static string GetString(string key, IConfiguration configuration, IConfiguration resolve, string def)
    {
        ArgumentGuard.NotNullOrEmpty(key);
        ArgumentGuard.NotNull(configuration);

        string val = configuration[key];

        if (!string.IsNullOrEmpty(val))
        {
            return PropertyPlaceholderHelper.ResolvePlaceholders(val, resolve);
        }

        return def;
    }
}
