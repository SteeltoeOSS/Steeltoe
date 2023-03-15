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

    public void MapFromTo(string fromKey, string toKey)
    {
        string value = GetFrom(fromKey);

        if (value != null)
        {
            SetToValue(toKey, value);
        }
    }

    public void MapFromTo(string fromKey, params string[] toKeySegments)
    {
        string value = GetFrom(fromKey);

        if (value != null)
        {
            string toKey = string.Join(ConfigurationPath.KeyDelimiter, toKeySegments);
            SetToValue(toKey, value);
        }
    }

    public void MapFromAppendTo(string fromKey, string appendToKey, string separator)
    {
        string valueToAppend = GetFrom(fromKey);

        if (valueToAppend != null)
        {
            string existingValue = GetTo(appendToKey);

            if (existingValue != null)
            {
                string newValue = $"{existingValue}{separator}{valueToAppend}";
                SetToValue(appendToKey, newValue);
            }
        }
    }

    public void MapFromToFile(string fromKey, string toKey)
    {
        string value = GetFrom(fromKey);

        if (value != null)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            using (StreamWriter writer = File.CreateText(tempPath))
            {
                writer.Write(value);
            }

            SetToValue(toKey, tempPath);
        }
    }

    public void SetToValue(string toKey, string value)
    {
        string key = $"{ToPrefix}{toKey}";
        ConfigurationData[key] = value;
    }

    public string GetFrom(string fromKey)
    {
        string key = $"{BindingKey}{fromKey}";
        return ConfigurationData.TryGetValue(key, out string value) ? value : null;
    }

    private string GetTo(string toKey)
    {
        string key = $"{ToPrefix}{toKey}";
        return ConfigurationData.TryGetValue(key, out string value) ? value : null;
    }
}
