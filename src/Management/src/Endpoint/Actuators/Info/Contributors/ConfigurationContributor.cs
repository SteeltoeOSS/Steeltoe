// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Info.Contributors;

internal abstract class ConfigurationContributor(IConfiguration? configuration)
{
    protected IConfiguration? Configuration { get; set; } = configuration;

    protected void Contribute(InfoBuilder builder, string prefix, bool keepPrefix)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(prefix);

        Dictionary<string, object?> dictionary = CreateDictionary(prefix, keepPrefix);
        builder.WithInfo(dictionary);
    }

    private Dictionary<string, object?> CreateDictionary(string prefix, bool keepPrefix)
    {
        var result = new Dictionary<string, object?>();

        if (Configuration != null)
        {
            Dictionary<string, object?> dictionary = result;

            IConfigurationSection section = Configuration.GetSection(prefix);
            IEnumerable<IConfigurationSection> childSections = section.GetChildren();

            if (keepPrefix)
            {
                dictionary = [];
                result[prefix] = dictionary;
            }

            AddChildren(dictionary, childSections);
        }

        return result;
    }

    private void AddChildren(Dictionary<string, object?> dictionary, IEnumerable<IConfigurationSection> sections)
    {
        foreach (IConfigurationSection section in sections)
        {
            if (section.Value == null)
            {
                var subDictionary = new Dictionary<string, object?>();
                dictionary[section.Key] = subDictionary;
                AddChildren(subDictionary, section.GetChildren());
            }
            else
            {
                AddKeyValue(dictionary, section.Key, section.Value);
            }
        }
    }

    protected virtual void AddKeyValue(IDictionary<string, object?> dictionary, string key, object? value)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentException.ThrowIfNullOrEmpty(key);

        dictionary[key] = value;
    }
}
