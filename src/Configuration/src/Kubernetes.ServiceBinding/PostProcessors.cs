// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding;

internal sealed class MySqlPostProcessor : IConfigurationPostProcessor
{
    internal const string BindingType = "mysql";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configurationData)
    {
        if (!provider.IsBindingTypeEnabled(BindingType))
        {
            return;
        }

        configurationData.Filter(ServiceBindingConfigurationProvider.InputKeyPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingType).ForEach(
            bindingNameKey =>
            {
                var mapper = new ServiceBindingMapper(configurationData, bindingNameKey, ServiceBindingConfigurationProvider.OutputKeyPrefix, BindingType,
                    ConfigurationPath.GetSectionKey(bindingNameKey));

                // See MySQL connection string parameters at: https://dev.mysql.com/doc/refman/8.0/en/connecting-using-uri-or-key-value-pairs.html#connection-parameters-base
                mapper.MapFromTo("host", "host");
                mapper.MapFromTo("port", "port");
                mapper.MapFromTo("database", "database");
                mapper.MapFromTo("username", "username");
                mapper.MapFromTo("password", "password");
            });
    }
}

internal sealed class PostgreSqlPostProcessor : IConfigurationPostProcessor
{
    internal const string BindingType = "postgresql";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configurationData)
    {
        if (!provider.IsBindingTypeEnabled(BindingType))
        {
            return;
        }

        configurationData.Filter(ServiceBindingConfigurationProvider.InputKeyPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingType).ForEach(
            bindingNameKey =>
            {
                var mapper = new ServiceBindingMapper(configurationData, bindingNameKey, ServiceBindingConfigurationProvider.OutputKeyPrefix, BindingType,
                    ConfigurationPath.GetSectionKey(bindingNameKey));

                // See PostgreSQL connection string parameters at: https://www.npgsql.org/doc/connection-string-parameters.html
                mapper.MapFromTo("host", "host");
                mapper.MapFromTo("port", "port");
                mapper.MapFromTo("database", "database");
                mapper.MapFromTo("username", "username");
                mapper.MapFromTo("password", "password");
            });
    }
}

internal sealed class RabbitMQPostProcessor : IConfigurationPostProcessor
{
    internal const string BindingType = "rabbitmq";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configurationData)
    {
        if (!provider.IsBindingTypeEnabled(BindingType))
        {
            return;
        }

        configurationData.Filter(ServiceBindingConfigurationProvider.InputKeyPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingType).ForEach(
            bindingNameKey =>
            {
                var mapper = new ServiceBindingMapper(configurationData, bindingNameKey, ServiceBindingConfigurationProvider.OutputKeyPrefix, BindingType,
                    ConfigurationPath.GetSectionKey(bindingNameKey));

                // See RabbitMQ connection string parameters at: https://www.rabbitmq.com/uri-spec.html
                mapper.MapFromTo("host", "host");
                mapper.MapFromTo("port", "port");
                mapper.MapFromTo("username", "username");
                mapper.MapFromTo("password", "password");
                mapper.MapFromTo("virtual-host", "virtualHost");
            });
    }
}

internal sealed class RedisPostProcessor : IConfigurationPostProcessor
{
    internal const string BindingType = "redis";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configurationData)
    {
        if (!provider.IsBindingTypeEnabled(BindingType))
        {
            return;
        }

        configurationData.Filter(ServiceBindingConfigurationProvider.InputKeyPrefix, ServiceBindingConfigurationProvider.TypeKey, BindingType).ForEach(
            bindingNameKey =>
            {
                var mapper = new ServiceBindingMapper(configurationData, bindingNameKey, ServiceBindingConfigurationProvider.OutputKeyPrefix, BindingType,
                    ConfigurationPath.GetSectionKey(bindingNameKey));

                // See Redis connection string parameters at: https://stackexchange.github.io/StackExchange.Redis/Configuration.html
                mapper.MapFromTo("host", "host");
                mapper.MapFromTo("port", "port");
                mapper.MapFromTo("ssl", "ssl");
                mapper.MapFromTo("password", "password");
                mapper.MapFromTo("database", "defaultDatabase");
                mapper.MapFromTo("client-name", "name");
            });
    }
}
