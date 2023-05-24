// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.PostProcessors;

internal sealed class RabbitMQKubernetesPostProcessor : IConfigurationPostProcessor
{
    internal const string BindingType = "rabbitmq";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configurationData)
    {
        foreach (string bindingKey in configurationData.Filter(KubernetesServiceBindingConfigurationProvider.FromKeyPrefix,
            KubernetesServiceBindingConfigurationProvider.TypeKey, BindingType))
        {
            var mapper = new ServiceBindingMapper(configurationData, bindingKey, KubernetesServiceBindingConfigurationProvider.ToKeyPrefix, BindingType,
                ConfigurationPath.GetSectionKey(bindingKey));

            // See RabbitMQ connection string parameters at: https://www.rabbitmq.com/uri-spec.html
            mapper.MapFromTo("host", "host");
            mapper.MapFromTo("port", "port");
            mapper.MapFromTo("username", "username");
            mapper.MapFromTo("password", "password");
            mapper.MapFromTo("virtual-host", "virtualHost");
        }
    }
}
