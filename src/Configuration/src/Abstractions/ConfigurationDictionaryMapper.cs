// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration;

internal abstract class ConfigurationDictionaryMapper
{
    public string BindingKey { get; }

    public string ToPrefix { get; }

    public IDictionary<string, string> ConfigurationData { get; }

    protected ConfigurationDictionaryMapper(IDictionary<string, string> configurationData, string bindingKey, params string[] toPrefix)
    {
        ConfigurationData = configurationData;
        BindingKey = !string.IsNullOrEmpty(bindingKey) ? bindingKey + ConfigurationPath.KeyDelimiter : string.Empty;

        if (toPrefix.Length > 0)
        {
            ToPrefix = string.Join(ConfigurationPath.KeyDelimiter, toPrefix) + ConfigurationPath.KeyDelimiter;
        }
    }

    public void MapFromTo(string existingKey, string newKey)
    {
        if (ConfigurationData.TryGetValue(BindingKey + existingKey, out string value))
        {
            if (ToPrefix != null)
            {
                ConfigurationData[ToPrefix + newKey] = value;
            }
            else
            {
                ConfigurationData[newKey] = value;
            }
        }
    }

    public void MapFromTo(string existingKey, params string[] newKeyPath)
    {
        if (ConfigurationData.TryGetValue(BindingKey + existingKey, out string value))
        {
            string newKey = string.Join(ConfigurationPath.KeyDelimiter, newKeyPath);

            if (ToPrefix != null)
            {
                ConfigurationData[ToPrefix + newKey] = value;
            }
            else
            {
                ConfigurationData[newKey] = value;
            }
        }
    }

    public void MapFromToFile(string existingKey, string newKey)
    {
        if (ConfigurationData.TryGetValue(BindingKey + existingKey, out string value))
        {
            string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            using (StreamWriter writer = File.CreateText(tempPath))
            {
                writer.Write(value);
            }

            if (ToPrefix != null)
            {
                ConfigurationData[ToPrefix + newKey] = tempPath;
            }
            else
            {
                ConfigurationData[newKey] = tempPath;
            }
        }
    }

    public void AddKeyValue(string newKey, string value)
    {
        ConfigurationData.Add(ToPrefix + newKey, value);
    }

    public string Get(string key)
    {
        return Get(key, null);
    }

    public string Get(string key, string defaultValue)
    {
        _ = ConfigurationData.TryGetValue(BindingKey + key, out string result);
        return result ?? defaultValue;
    }
}
