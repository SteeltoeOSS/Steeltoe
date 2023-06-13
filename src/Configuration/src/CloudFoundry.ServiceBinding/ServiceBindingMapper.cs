// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding;

internal sealed class ServiceBindingMapper : ConfigurationDictionaryMapper
{
    public string BindingProvider { get; }
    public string BindingName { get; }
    public string BindingType { get; }

    private ServiceBindingMapper(IDictionary<string, string> configurationData, string bindingKey, string bindingType, string bindingProvider,
        string bindingName, string[] toPrefix)
        : base(configurationData, bindingKey, toPrefix)
    {
        BindingProvider = bindingProvider;
        BindingName = bindingName;
        BindingType = bindingType;
    }

    public static ServiceBindingMapper Create(IDictionary<string, string> configurationData, string bindingKey, string bindingType)
    {
        ArgumentGuard.NotNull(bindingType);
        ArgumentGuard.NotNull(bindingKey);
        ArgumentGuard.NotNull(configurationData);

        string bindingName = configurationData[ConfigurationPath.Combine(bindingKey, "name")];
        string bindingProvider = ConfigurationPath.GetSectionKey(ConfigurationPath.GetParentPath(bindingKey));

        List<string> toPrefix = CloudFoundryServiceBindingConfigurationProvider.ToKeyPrefix.Split(ConfigurationPath.KeyDelimiter).ToList();
        toPrefix.Add(bindingType);
        toPrefix.Add(bindingName);

        return new ServiceBindingMapper(configurationData, bindingKey, bindingType, bindingProvider, bindingName, toPrefix.ToArray());
    }
}
