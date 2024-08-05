// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding;

internal sealed class ServiceBindingMapper : ConfigurationDictionaryMapper
{
    public string BindingProvider { get; }
    public string BindingName { get; }
    public string BindingType { get; }

    private ServiceBindingMapper(IDictionary<string, string?> configurationData, string bindingKey, string bindingType, string bindingProvider,
        string bindingName, string[] toPrefix)
        : base(configurationData, bindingKey, toPrefix)
    {
        BindingProvider = bindingProvider;
        BindingName = bindingName;
        BindingType = bindingType;
    }

    public static ServiceBindingMapper Create(IDictionary<string, string?> configurationData, string bindingKey, string bindingType,
        string? overrideToPrefix = null)
    {
        ArgumentNullException.ThrowIfNull(configurationData);
        ArgumentException.ThrowIfNullOrEmpty(bindingKey);
        ArgumentException.ThrowIfNullOrEmpty(bindingType);

        string bindingName = configurationData[ConfigurationPath.Combine(bindingKey, "name")] ?? string.Empty;
        string bindingProvider = ConfigurationPath.GetSectionKey(ConfigurationPath.GetParentPath(bindingKey)) ?? string.Empty;
        string[] toPrefix = CreateToPrefix(bindingType, bindingName, overrideToPrefix);

        return new ServiceBindingMapper(configurationData, bindingKey, bindingType, bindingProvider, bindingName, toPrefix);
    }

    private static string[] CreateToPrefix(string bindingType, string bindingName, string? overrideToPrefix)
    {
        if (overrideToPrefix != null)
        {
            return overrideToPrefix.Split(ConfigurationPath.KeyDelimiter).ToArray();
        }

        List<string> toPrefix = CloudFoundryServiceBindingConfigurationProvider.ToKeyPrefix.Split(ConfigurationPath.KeyDelimiter).ToList();
        toPrefix.Add(bindingType);
        toPrefix.Add(bindingName);
        return toPrefix.ToArray();
    }
}
