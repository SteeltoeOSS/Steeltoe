// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding;

internal abstract class CloudFoundryConfigurationPostProcessor : IConfigurationPostProcessor
{
    private static readonly Regex TagsConfigurationKeyRegex = new(@"^vcap:services:[^:]+:[0-9]+:tags:[0-9]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public abstract void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configurationData);

    protected IEnumerable<string> FilterKeys(IDictionary<string, string> configurationData, string tagValueToFind)
    {
        List<string> keys = new();

        foreach ((string key, string value) in configurationData)
        {
            if (TagsConfigurationKeyRegex.IsMatch(key) && string.Equals(value, tagValueToFind, StringComparison.OrdinalIgnoreCase))
            {
                string serviceBindingKey = ConfigurationPath.GetParentPath(ConfigurationPath.GetParentPath(key));
                keys.Add(serviceBindingKey);
            }
        }

        return keys;
    }
}
