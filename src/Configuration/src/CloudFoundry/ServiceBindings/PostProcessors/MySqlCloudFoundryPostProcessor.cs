// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry.ServiceBindings.PostProcessors;

internal sealed class MySqlCloudFoundryPostProcessor : CloudFoundryPostProcessor
{
    internal const string BindingType = "mysql";

    public override void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string?> configurationData)
    {
        foreach (string key in FilterKeys(configurationData, BindingType, KeyFilterSources.Tag))
        {
            var mapper = ServiceBindingMapper.Create(configurationData, key, BindingType);

            // Mapping from CloudFoundry service binding credentials to driver-specific connection string parameters.
            // The available credentials are documented at:
            // - Tanzu Broker: https://techdocs.broadcom.com/us/en/vmware-tanzu/data-solutions/tanzu-for-mysql-on-cloud-foundry/10-0/mysql-for-tpcf/use.html#vcap
            // - GCP Service Broker: https://techdocs.broadcom.com/us/en/vmware-tanzu/platform-services/tanzu-cloud-service-broker-for-gcp/1-9/csb-gcp/reference-gcp-mysql.html#binding-creds
            // - AWS Service Broker: https://techdocs.broadcom.com/us/en/vmware-tanzu/platform-services/tanzu-cloud-service-broker-for-aws/1-14/csb-aws/reference-aws-mysql.html#binding-creds

            mapper.MapFromTo("credentials:hostname", "host");
            mapper.MapFromTo("credentials:port", "port");
            mapper.MapFromTo("credentials:name", "database");
            mapper.MapFromTo("credentials:username", "username");
            mapper.MapFromTo("credentials:password", "password");
            mapper.MapFromToFile("credentials:sslCert", "ssl-cert");
            mapper.MapFromToFile("credentials:sslKey", "ssl-key");
            mapper.MapFromToFile("credentials:sslRootCert", "ssl-ca");
        }
    }
}
