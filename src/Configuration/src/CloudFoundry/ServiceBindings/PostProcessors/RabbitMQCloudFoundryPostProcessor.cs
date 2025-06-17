// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry.ServiceBindings.PostProcessors;

internal sealed class RabbitMQCloudFoundryPostProcessor : CloudFoundryPostProcessor
{
    internal const string BindingType = "rabbitmq";

    public override void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string?> configurationData)
    {
        foreach (string key in FilterKeys(configurationData, BindingType, KeyFilterSources.Tag))
        {
            var mapper = ServiceBindingMapper.Create(configurationData, key, BindingType);

            // Mapping from CloudFoundry service binding credentials to driver-specific connection string parameters.
            // The available credentials are documented at:
            // - Tanzu Broker: https://techdocs.broadcom.com/us/en/vmware-tanzu/data-solutions/tanzu-rabbitmq-on-cloud-foundry/10-0/tanzu-rabbitmq-cloud-foundry/reference.html

            string? useTlsValue = mapper.MapFromTo("credentials:ssl", "useTls");
            string fromProtocol = bool.TryParse(useTlsValue, out bool useTls) && useTls ? "amqp+ssl" : "amqp";

            mapper.MapFromTo($"credentials:protocols:{fromProtocol}:host", "host");
            mapper.MapFromTo($"credentials:protocols:{fromProtocol}:port", "port");
            mapper.MapFromTo($"credentials:protocols:{fromProtocol}:username", "username");
            mapper.MapFromTo($"credentials:protocols:{fromProtocol}:password", "password");
            mapper.MapFromTo($"credentials:protocols:{fromProtocol}:vhost", "virtualHost");
        }
    }
}
