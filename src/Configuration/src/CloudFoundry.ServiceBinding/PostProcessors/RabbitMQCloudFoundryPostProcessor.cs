// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding.PostProcessors;

internal sealed class RabbitMQCloudFoundryPostProcessor : CloudFoundryPostProcessor
{
    internal const string BindingType = "rabbitmq";

    public override void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configurationData)
    {
        foreach (string key in FilterKeys(configurationData, BindingType))
        {
            var mapper = ServiceBindingMapper.Create(configurationData, key, BindingType);

            // See RabbitMQ connection string parameters at: https://www.rabbitmq.com/uri-spec.html
            string useTlsValue = mapper.MapFromTo("credentials:ssl", "useTls");

            string fromProtocol = bool.TryParse(useTlsValue, out bool useTls) && useTls ? "amqp+ssl" : "amqp";

            mapper.MapFromTo($"credentials:protocols:{fromProtocol}:host", "host");
            mapper.MapFromTo($"credentials:protocols:{fromProtocol}:port", "port");
            mapper.MapFromTo($"credentials:protocols:{fromProtocol}:username", "username");
            mapper.MapFromTo($"credentials:protocols:{fromProtocol}:password", "password");
            mapper.MapFromTo($"credentials:protocols:{fromProtocol}:vhost", "virtualHost");
        }
    }
}
