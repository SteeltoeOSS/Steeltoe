// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Management.Info;

public abstract class ConfigurationContributor
{
    protected IConfiguration Configuration { get; set; }

    protected ConfigurationContributor()
    {
    }

    protected ConfigurationContributor(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    protected virtual void Contribute(IInfoBuilder builder, string prefix, bool keepPrefix)
    {
        ArgumentGuard.NotNull(builder);

        builder.WithInfo(CreateDictionary(prefix, keepPrefix));
    }

    protected virtual Dictionary<string, object> CreateDictionary(string prefix, bool keepPrefix)
    {
        var result = new Dictionary<string, object>();

        if (Configuration != null)
        {
            Dictionary<string, object> dict = result;

            IConfigurationSection section = Configuration.GetSection(prefix);
            IEnumerable<IConfigurationSection> children = section.GetChildren();

            if (keepPrefix)
            {
                result[prefix] = dict = new Dictionary<string, object>();
            }

            AddChildren(dict, children);
        }

        return result;
    }

    protected virtual void AddChildren(Dictionary<string, object> dictionary, IEnumerable<IConfigurationSection> sections)
    {
        ArgumentGuard.NotNull(dictionary);
        ArgumentGuard.NotNull(sections);

        foreach (IConfigurationSection section in sections)
        {
            string key = section.Key;
            string value = section.Value;

            if (value == null)
            {
                var emptyDictionary = new Dictionary<string, object>();
                dictionary[key] = emptyDictionary;
                AddChildren(emptyDictionary, section.GetChildren());
            }
            else
            {
                AddKeyValue(dictionary, key, value);
            }
        }
    }

    protected virtual void AddKeyValue(Dictionary<string, object> dictionary, string key, string value)
    {
        ArgumentGuard.NotNull(dictionary);
        ArgumentGuard.NotNull(key);

        dictionary[key] = value;
    }
}
