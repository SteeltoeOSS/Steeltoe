// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Configuration;

internal abstract class ConfigurationDictionaryMapper
{
    public string BindingKey { get; }

    public string ToPrefix { get; }

    public IDictionary<string, string> ConfigData { get; }

    protected ConfigurationDictionaryMapper(IDictionary<string, string> configData, string bindingKey, params string[] toPrefix)
    {
        ConfigData = configData;
        BindingKey = !string.IsNullOrEmpty(bindingKey) ? bindingKey + ConfigurationPath.KeyDelimiter : string.Empty;

        if (toPrefix.Length > 0)
        {
            ToPrefix = string.Join(ConfigurationPath.KeyDelimiter, toPrefix) + ConfigurationPath.KeyDelimiter;
        }
    }

    public void MapFromTo(string existingKey, string newKey)
    {
        if (ConfigData.TryGetValue(BindingKey + existingKey, out string value))
        {
            if (ToPrefix != null)
            {
                ConfigData[ToPrefix + newKey] = value;
            }
            else
            {
                ConfigData[newKey] = value;
            }
        }
    }

    public void MapFromTo(string existingKey, params string[] newKeyPath)
    {
        if (ConfigData.TryGetValue(BindingKey + existingKey, out string value))
        {
            string newKey = string.Join(ConfigurationPath.KeyDelimiter, newKeyPath);

            if (ToPrefix != null)
            {
                ConfigData[ToPrefix + newKey] = value;
            }
            else
            {
                ConfigData[newKey] = value;
            }
        }
    }

    /// <summary>
    /// Finds configuration entries under the <see cref="BindingKey" />, transfers them to <see cref="ToPrefix" />.
    /// Can convert from Spring style to .NET style hierarchical paths.
    /// </summary>
    /// <param name="configurationKeys">
    /// List of keys to map.
    /// </param>
    /// <param name="convertSpringToNetDelimiters">
    /// Determine whether keys should be converted from Spring-style to .NET style (period vs colon for hierarchy)
    /// </param>
    public void MapFrom(IEnumerable<string> configurationKeys, bool convertSpringToNetDelimiters)
    {
        foreach (string key in configurationKeys)
        {
            if (!key.Equals("type", StringComparison.InvariantCultureIgnoreCase) && !key.Equals("provider", StringComparison.InvariantCultureIgnoreCase))
            {
                string value = Get(key);

                if (!convertSpringToNetDelimiters)
                {
                    AddKeyValue(key, value);
                }
                else
                {
                    AddKeyValue(key.Replace('.', ':'), value);
                }
            }
        }
    }

    public void AddKeyValue(string newKey, string value)
    {
        ConfigData.Add(ToPrefix + newKey, value);
    }

    public string Get(string key)
    {
        return Get(key, null);
    }

    public string Get(string key, string defaultValue)
    {
        _ = ConfigData.TryGetValue(BindingKey + key, out string result);
        return result ?? defaultValue;
    }
}
