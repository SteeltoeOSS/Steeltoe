// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry.ServiceBindings.PostProcessors;

internal sealed class RedisCloudFoundryPostProcessor : CloudFoundryPostProcessor
{
    internal const string BindingType = "redis";

    public override void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string?> configurationData)
    {
        foreach (string key in FilterKeys(configurationData, BindingType, KeyFilterSources.Tag))
        {
            var mapper = ServiceBindingMapper.Create(configurationData, key, BindingType);

            // Mapping from CloudFoundry service binding credentials to driver-specific connection string parameters.
            // The available credentials are documented at:
            // - Tanzu Broker: https://techdocs.broadcom.com/us/en/vmware-tanzu/platform/tanzu-valkey-tanzu-platform/10-2/valkey-tp/using.html#use-valkey-service-in-app
            // - Azure Service Broker: https://techdocs.broadcom.com/us/en/vmware-tanzu/platform/cloud-service-broker-azure/1-13/csb-azure/reference-azure-redis.html
            // - AWS Service Broker: https://techdocs.broadcom.com/us/en/vmware-tanzu/platform/cloud-service-broker-aws/1-15/csb-aws/reference-aws-redis.html

            mapper.MapFromTo("credentials:host", "host");
            mapper.MapFromTo("credentials:port", "port");
            mapper.SetToValue("user", null);
            mapper.MapFromTo("credentials:password", "password");

            if (mapper.GetFromValue("credentials:tls_port") != null)
            {
                mapper.MapFromTo("credentials:tls_port", "port");
                mapper.SetToValue("ssl", "true");
            }
        }
    }
}
