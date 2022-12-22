// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
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
