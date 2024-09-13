// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.CloudFoundry.ServiceBindings.PostProcessors;

internal abstract class CloudFoundryPostProcessor : IConfigurationPostProcessor
{
    private static readonly Regex TagsConfigurationKeyRegex =
        new("^vcap:services:[^:]+:[0-9]+:tags:[0-9]+", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));

    private static readonly Regex LabelConfigurationKeyRegex =
        new("^vcap:services:[^:]+:[0-9]+:label+", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));

    public abstract void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string?> configurationData);

    protected IEnumerable<string> FilterKeys(IDictionary<string, string?> configurationData, string valueToFind, KeyFilterSources sources)
    {
        List<string> keys = [];

        foreach ((string key, string? value) in configurationData)
        {
            if ((sources & KeyFilterSources.Tag) != 0 && TagsConfigurationKeyRegex.IsMatch(key) &&
                string.Equals(value, valueToFind, StringComparison.OrdinalIgnoreCase))
            {
                string? parentKey = ConfigurationPath.GetParentPath(key);

                if (parentKey != null)
                {
                    string? serviceBindingKey = ConfigurationPath.GetParentPath(parentKey);

                    if (serviceBindingKey != null)
                    {
                        keys.Add(serviceBindingKey);
                    }
                }
            }

            if ((sources & KeyFilterSources.Label) != 0 && LabelConfigurationKeyRegex.IsMatch(key) &&
                string.Equals(value, valueToFind, StringComparison.OrdinalIgnoreCase))
            {
                string? serviceBindingKey = ConfigurationPath.GetParentPath(key);

                if (serviceBindingKey != null)
                {
                    keys.Add(serviceBindingKey);
                }
            }
        }

        return keys;
    }

    [Flags]
    internal enum KeyFilterSources
    {
        Tag = 1,
        Label = 2
    }
}
