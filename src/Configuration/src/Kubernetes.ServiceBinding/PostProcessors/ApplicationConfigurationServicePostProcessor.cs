// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Configuration;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.PostProcessors;

internal sealed class ApplicationConfigurationServicePostProcessor : IConfigurationPostProcessor
{
    internal const string BindingType = "config";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string?> configurationData)
    {
        foreach (string bindingKey in configurationData.Filter(KubernetesServiceBindingConfigurationProvider.FromKeyPrefix,
            KubernetesServiceBindingConfigurationProvider.TypeKey, BindingType))
        {
            var mapper = new ServiceBindingMapper(configurationData, bindingKey);

            IEnumerable<string> keysToMap = configurationData.Keys
                .Select(s => s.Split($"{bindingKey}:")[^1])
                .Except([KubernetesServiceBindingConfigurationProvider.ProviderKey, KubernetesServiceBindingConfigurationProvider.TypeKey])
                .ToList();

            foreach (var key in keysToMap)
            {
                mapper.MapFromTo(key, key.AsDotNetConfigurationKey());
            }
        }
    }
}
