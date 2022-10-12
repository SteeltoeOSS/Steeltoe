// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding;
internal sealed class ConfigServerPostProcessor : IConfigurationPostProcessor
{
    public void PostProcessConfiguration(IConfigurationProvider provider, IDictionary<string, string> configData)
    {
        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, "config")
            .ForEach((bindingNameKey) =>
            {
                var mapper = new Mapper(configData, bindingNameKey, "spring", "cloud", "config");
                mapper.MapFromTo("uri", "uri");
                mapper.MapFromTo("client-id", "client", "oauth2", "clientId");
                mapper.MapFromTo("client-secret", "client", "oauth2", "clientSecret");
                mapper.MapFromTo("access-token-uri", "client", "oauth2", "accessTokenUri");
            });
    }
}

internal sealed class RabbitMQPostProcessor : IConfigurationPostProcessor
{
    public void PostProcessConfiguration(IConfigurationProvider provider, IDictionary<string, string> configData)
    {
        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, "rabbitmq")
            .ForEach((bindingNameKey) =>
            {
                var mapper = new Mapper(configData, bindingNameKey, "spring", "rabbitmq");
                mapper.MapFromTo("addresses", "addresses");
                mapper.MapFromTo("host", "host");
                mapper.MapFromTo("password", "password");
                mapper.MapFromTo("port", "port");
                mapper.MapFromTo("username", "username");
                mapper.MapFromTo("virtual-host", "virtualhost");
            });
    }
}

internal sealed class RedisPostProcessor : IConfigurationPostProcessor
{
    public void PostProcessConfiguration(IConfigurationProvider provider, IDictionary<string, string> configData)
    {
        configData.Filter(ServiceBindingConfigurationProvider.KubernetesBindingsPrefix, ServiceBindingConfigurationProvider.TypeKey, "redis")
            .ForEach((bindingNameKey) =>
            {
                var mapper = new Mapper(configData, bindingNameKey, "spring", "redis");
                mapper.MapFromTo("client-name", "clientname");
                    //mapper.MapFromTo("cluster.max-redirects").to("spring.redis.cluster.max-redirects");
                    //mapper.MapFromTo("cluster.nodes").to("spring.redis.cluster.nodes");
                    //mapper.MapFromTo("database").to("spring.redis.database");
                mapper.MapFromTo("host", "host");
                mapper.MapFromTo("password", "password");
                mapper.MapFromTo("port", "port");
                    //mapper.MapFromTo("sentinel.master").to("spring.redis.sentinel.master");
                    //mapper.MapFromTo("sentinel.nodes").to("spring.redis.sentinel.nodes");
                mapper.MapFromTo("ssl", "ssl");
                    //mapper.MapFromTo("url").to("spring.redis.url");
            });
    }
}
