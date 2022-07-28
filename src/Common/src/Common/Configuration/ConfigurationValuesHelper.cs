// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Linq;

namespace Steeltoe.Common.Configuration;

public static class ConfigurationValuesHelper
{
    public static string GetSetting(string key, IConfiguration primary, IConfiguration secondary, IConfiguration resolve, string def)
    {
        // First check for key in primary
        var setting = GetString(key, primary, resolve, null);
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
    /// Get setting from config searching the given configPrefix keys in order. Returns the first element with key.
    /// </summary>
    /// <param name="key">The key of the element to return.</param>
    /// <param name="config">IConfiguration to search through.</param>
    /// <param name="defaultValue">The default Value if no configuration is found.</param>
    /// <param name="configPrefixes">The prefixes to search for in given order.</param>
    /// <returns>Config value.</returns>
    public static string GetSetting(string key, IConfiguration config, string defaultValue, params string[] configPrefixes)
    {
        foreach (var prefix in configPrefixes)
        {
            var section = config.GetSection(prefix);
            var result = section.GetValue<string>(key);
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// Get a setting from config by searching the given keys in order. Returns the first match.
    /// </summary>
    /// <param name="config">IConfiguration to search through.</param>
    /// <param name="defaultValue">The default Value if no configuration is found.</param>
    /// <param name="configKeys">The fully-qualified keys to search for in given order.</param>
    /// <returns>Value from config or default (if not found).</returns>
    public static string GetPreferredSetting(IConfiguration config, string defaultValue, params string[] configKeys)
    {
        foreach (var key in configKeys.Where(c => !string.IsNullOrEmpty(c)))
        {
            var result = config.GetValue<string>(key);
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }
        }

        return defaultValue;
    }

    public static int GetInt(string key, IConfiguration config, IConfiguration resolve, int def)
    {
        var val = GetString(key, config, resolve, null);
        if (!string.IsNullOrEmpty(val) && int.TryParse(val, out var result))
        {
            return result;
        }

        return def;
    }

    public static double GetDouble(string key, IConfiguration config, IConfiguration resolve, double def)
    {
        var val = GetString(key, config, resolve, null);
        if (!string.IsNullOrEmpty(val) && double.TryParse(val, out var result))
        {
            return result;
        }

        return def;
    }

    public static bool GetBoolean(string key, IConfiguration config, IConfiguration resolve, bool def)
    {
        var val = GetString(key, config, resolve, null);
        if (!string.IsNullOrEmpty(val) && bool.TryParse(val, out var result))
        {
            return result;
        }

        return def;
    }

    public static string GetString(string key, IConfiguration config, IConfiguration resolve, string def)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException(nameof(key));
        }

        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        var val = config[key];
        if (!string.IsNullOrEmpty(val))
        {
            return PropertyPlaceholderHelper.ResolvePlaceholders(val, resolve);
        }

        return def;
    }
}
