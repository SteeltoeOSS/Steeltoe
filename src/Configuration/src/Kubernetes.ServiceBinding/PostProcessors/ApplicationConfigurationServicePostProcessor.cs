// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

            IEnumerable<string> keysToMap = configurationData.Keys.Select(s => s.Split($"{bindingKey}:").Last()).ToList();
            mapper.MapFrom(keysToMap);
        }
    }
}
