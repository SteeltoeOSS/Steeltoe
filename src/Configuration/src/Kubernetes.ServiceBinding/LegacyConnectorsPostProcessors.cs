// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding;

internal sealed class RabbitMQLegacyConnectorPostProcessor : IConfigurationPostProcessor
{
    internal const string BindingTypeKey = "rabbitmq";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configData)
    {
        if (!provider.IsBindingTypeEnabled(BindingTypeKey))
        {
            return;
        }

        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingTypeKey)
            .ForEach((bindingNameKey) =>
            {
                // Spring -> spring.rabbitmq....
                // Steeltoe -> rabbitmq:client:....
                var mapper = new ServiceBindingMapper(configData, bindingNameKey, "rabbitmq", "client");
                mapper.MapFromTo("addresses", "uri");
                mapper.MapFromTo("host", "server");
                mapper.MapFromTo("password", "password");
                mapper.MapFromTo("port", "port");
                mapper.MapFromTo("username", "username");
                mapper.MapFromTo("virtual-host", "virtualhost");
            });
    }
}
internal sealed class MySqlLegacyConnectorPostProcessor : IConfigurationPostProcessor
{
    internal const string BindingTypeKey = "mysql";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configData)
    {
        if (!provider.IsBindingTypeEnabled(BindingTypeKey))
        {
            return;
        }

        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingTypeKey)
            .ForEach((bindingNameKey) =>
            {
                // Spring -> spring.datasource....
                // Steeltoe -> steeltoe:mysql:binding-name....
                var mapper = new ServiceBindingMapper(configData, bindingNameKey, "mysql", "client");
                mapper.MapFromTo("username", "username");
                mapper.MapFromTo("password", "password");
                mapper.MapFromTo("host", "server");
                mapper.MapFromTo("port", "port");
                mapper.MapFromTo("database", "database");

                // Spring indicates this takes precedence over above
                mapper.MapFromTo("jdbc-url", "jdbcUrl");

                // Note: Spring also adds spring.r2dbc.... properties as well
            });
    }
}
