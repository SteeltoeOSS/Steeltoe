// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding.PostProcessors;

internal abstract class CloudFoundryPostProcessor : IConfigurationPostProcessor
{
    private static readonly Regex TagsConfigurationKeyRegex = new(@"^vcap:services:[^:]+:[0-9]+:tags:[0-9]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public abstract void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string?> configurationData);

    protected ICollection<string> FilterKeys(IDictionary<string, string?> configurationData, string tagValueToFind)
    {
        List<string> keys = new();

        foreach ((string key, string? value) in configurationData)
        {
            if (TagsConfigurationKeyRegex.IsMatch(key) && string.Equals(value, tagValueToFind, StringComparison.OrdinalIgnoreCase))
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
        }

        return keys;
    }
}
