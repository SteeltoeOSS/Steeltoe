// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.Kubernetes.ServiceBindings.PostProcessors;

internal sealed class RabbitMQKubernetesPostProcessor : IConfigurationPostProcessor
{
    internal const string BindingType = "rabbitmq";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string?> configurationData)
    {
        foreach (string bindingKey in configurationData.FilterKeys(KubernetesServiceBindingConfigurationProvider.FromKeyPrefix,
            KubernetesServiceBindingConfigurationProvider.TypeKey, BindingType))
        {
            var mapper = new ServiceBindingMapper(configurationData, bindingKey, KubernetesServiceBindingConfigurationProvider.ToKeyPrefix, BindingType,
                ConfigurationPath.GetSectionKey(bindingKey));

            // Mapping from Kubernetes secrets to driver-specific connection string parameters.
            // At the time of writing (June 2023), there's no complete official documentation for the available secrets. Some pointers:
            // - Generic secrets: https://github.com/servicebinding/spec#well-known-secret-entries
            // - Input keys used at https://github.com/spring-cloud/spring-cloud-bindings/blob/main/spring-cloud-bindings/src/main/java/org/springframework/cloud/bindings/boot/RabbitMqBindingsPropertiesProcessor.java

            mapper.SetToValue("useTls", "false");
            mapper.MapFromTo("host", "host");
            mapper.MapFromTo("port", "port");
            mapper.MapFromTo("username", "username");
            mapper.MapFromTo("password", "password");
            mapper.MapFromTo("virtual-host", "virtualHost");
        }
    }
}
