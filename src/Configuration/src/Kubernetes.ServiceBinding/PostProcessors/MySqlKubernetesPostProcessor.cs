// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.PostProcessors;

internal sealed class MySqlKubernetesPostProcessor : IConfigurationPostProcessor
{
    internal const string BindingType = "mysql";

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configurationData)
    {
        foreach (string bindingKey in configurationData.Filter(KubernetesServiceBindingConfigurationProvider.FromKeyPrefix,
            KubernetesServiceBindingConfigurationProvider.TypeKey, BindingType))
        {
            var mapper = new ServiceBindingMapper(configurationData, bindingKey, KubernetesServiceBindingConfigurationProvider.ToKeyPrefix, BindingType,
                ConfigurationPath.GetSectionKey(bindingKey));

            // See MySQL connection string parameters at: https://dev.mysql.com/doc/refman/8.0/en/connecting-using-uri-or-key-value-pairs.html#connection-parameters-base
            mapper.MapFromTo("host", "host");
            mapper.MapFromTo("port", "port");
            mapper.MapFromTo("database", "database");
            mapper.MapFromTo("username", "username");
            mapper.MapFromTo("password", "password");
        }
    }
}
