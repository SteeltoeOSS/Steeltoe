// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding;

internal sealed class ServiceBindingMapper : ConfigurationDictionaryMapper
{
    public string BindingProvider { get; }
    public string BindingName { get; }
    public string BindingType { get; }

    public ServiceBindingMapper(IDictionary<string, string?> configurationData, string bindingKey, params string[] toPrefix)
        : base(configurationData, bindingKey, toPrefix)
    {
        BindingProvider = GetBindingProvider($"{BindingKey}provider");
        BindingType = GetBindingType($"{BindingKey}type");
        BindingName = ConfigurationPath.GetSectionKey(bindingKey);
    }

    private string GetBindingProvider(string providerKey)
    {
        return !ConfigurationData.TryGetValue(providerKey, out string? result) || result == null ? string.Empty : result;
    }

    private string GetBindingType(string typeKey)
    {
        return !ConfigurationData.TryGetValue(typeKey, out string? result) || result == null ? string.Empty : result;
    }
}
