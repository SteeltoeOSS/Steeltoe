// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.PostProcessors;

internal sealed class RedisKubernetesPostProcessor : IConfigurationPostProcessor
{
    internal const string BindingType = "redis";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configurationData)
    {
        foreach (string bindingKey in configurationData.Filter(KubernetesServiceBindingConfigurationProvider.FromKeyPrefix,
            KubernetesServiceBindingConfigurationProvider.TypeKey, BindingType))

        {
            var mapper = new ServiceBindingMapper(configurationData, bindingKey, KubernetesServiceBindingConfigurationProvider.ToKeyPrefix, BindingType,
                ConfigurationPath.GetSectionKey(bindingKey));

            // See Redis connection string parameters at: https://stackexchange.github.io/StackExchange.Redis/Configuration.html
            mapper.MapFromTo("host", "host");
            mapper.MapFromTo("port", "port");
            mapper.MapFromTo("ssl", "ssl");
            mapper.MapFromTo("password", "password");
            mapper.MapFromTo("database", "defaultDatabase");
            mapper.MapFromTo("client-name", "name");
        }
    }
}
