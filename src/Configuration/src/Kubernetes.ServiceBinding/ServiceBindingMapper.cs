// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Configuration.Kubernetes.ServiceBinding;

internal class ServiceBindingMapper : ConfigurationDictionaryMapper
{
    public string BindingProvider { get; }

    public string BindingName { get; }

    public string BindingType { get; }

    public ServiceBindingMapper(IDictionary<string, string> configData, string bindingKey, params string[] toPrefix)
        : base(configData, bindingKey, toPrefix)
    {
        BindingProvider = GetBindingProvider(BindingKey + "provider");
        BindingType = GetBindingType(BindingKey + "type");
        BindingName = ConfigurationPath.GetSectionKey(bindingKey);
    }

    private string GetBindingProvider(string providerKey)
    {
        return ConfigData.TryGetValue(providerKey, out string result) ? result : string.Empty;
    }

    private string GetBindingType(string typeKey)
    {
        return ConfigData.TryGetValue(typeKey, out string result) ? result : string.Empty;
    }
}
