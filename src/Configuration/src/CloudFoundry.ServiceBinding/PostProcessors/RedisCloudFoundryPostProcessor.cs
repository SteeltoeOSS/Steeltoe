// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding.PostProcessors;

internal sealed class RedisCloudFoundryPostProcessor : CloudFoundryPostProcessor
{
    internal const string BindingType = "redis";

    public override void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string?> configurationData)
    {
        foreach (string key in FilterKeys(configurationData, BindingType))
        {
            var mapper = ServiceBindingMapper.Create(configurationData, key, BindingType);

            // Mapping from CloudFoundry service binding credentials to driver-specific connection string parameters.
            // The available credentials are documented at:
            // - VMware Broker: https://docs.vmware.com/en/Redis-for-VMware-Tanzu-Application-Service/3.1/redis-tanzu-application-service/GUID-using.html#use-the-redis-service-in-your-app-13
            // - Azure Service Broker: https://docs.vmware.com/en/Tanzu-Cloud-Service-Broker-for-Azure/1.4/csb-azure/GUID-reference-azure-redis.html#binding-credentials-3
            // - GCP Service Broker: https://docs.vmware.com/en/Tanzu-Cloud-Service-Broker-for-GCP/1.2/csb-gcp/GUID-reference-gcp-redis.html#binding-credentials-2
            // - AWS Service Broker: https://docs.vmware.com/en/Tanzu-Cloud-Service-Broker-for-AWS/1.5/csb-aws/GUID-reference-aws-redis.html#binding-credentials-3

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
