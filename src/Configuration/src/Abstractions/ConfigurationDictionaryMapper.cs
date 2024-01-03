// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Text;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration;

internal abstract class ConfigurationDictionaryMapper
{
    protected string BindingKey { get; }
    protected string ToPrefix { get; }
    protected IDictionary<string, string?> ConfigurationData { get; }

    protected ConfigurationDictionaryMapper(IDictionary<string, string?> configurationData, string bindingKey, params string[] toPrefix)
    {
        ConfigurationData = configurationData;
        BindingKey = !string.IsNullOrEmpty(bindingKey) ? bindingKey + ConfigurationPath.KeyDelimiter : string.Empty;
        ToPrefix = toPrefix.Length > 0 ? string.Join(ConfigurationPath.KeyDelimiter, toPrefix) + ConfigurationPath.KeyDelimiter : string.Empty;
    }

    public string? MapFromTo(string fromKey, string toKey)
    {
        string? value = GetFromValue(fromKey);
        SetToValue(toKey, value);

        return value;
    }

    public string? MapArrayFromTo(string fromArrayKey, string toKey, string separator)
    {
        int arrayIndex = 0;
        var toValueBuilder = new StringBuilder();

        while (true)
        {
            string arrayElementKey = $"{BindingKey}{fromArrayKey}:{arrayIndex}";

            if (!ConfigurationData.TryGetValue(arrayElementKey, out string? arrayElementValue))
            {
                break;
            }

            if (!string.IsNullOrEmpty(arrayElementValue))
            {
                if (toValueBuilder.Length > 0)
                {
                    toValueBuilder.Append(separator);
                }

                toValueBuilder.Append(arrayElementValue);
            }

            arrayIndex++;
        }

        if (toValueBuilder.Length > 0)
        {
            string toValue = toValueBuilder.ToString();
            SetToValue(toKey, toValue);
            return toValue;
        }

        return null;
    }

    public string? MapFromAppendTo(string fromKey, string appendToKey, string separator)
    {
        string? valueToAppend = GetFromValue(fromKey);

        if (valueToAppend != null)
        {
            string? existingValue = GetToValue(appendToKey);

            if (existingValue != null)
            {
                string newValue = $"{existingValue}{separator}{valueToAppend}";
                SetToValue(appendToKey, newValue);

                return newValue;
            }
        }

        return null;
    }

    public string? MapFromToFile(string fromKey, string toKey)
    {
        string? value = GetFromValue(fromKey);

        if (value == null)
        {
            SetToValue(toKey, null);
            return null;
        }

        string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        using (StreamWriter writer = File.CreateText(tempPath))
        {
            writer.Write(value);
        }

        SetToValue(toKey, tempPath);
        return tempPath;
    }

    public void SetToValue(string toKey, string? value)
    {
        string key = $"{ToPrefix}{toKey}";
        ConfigurationData[key] = value;
    }

    public string? GetFromValue(string fromKey)
    {
        string key = $"{BindingKey}{fromKey}";
        return ConfigurationData.TryGetValue(key, out string? value) ? value : null;
    }

    private string? GetToValue(string toKey)
    {
        string key = $"{ToPrefix}{toKey}";
        return ConfigurationData.TryGetValue(key, out string? value) ? value : null;
    }
}
