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
            // - VMware Broker: https://docs.vmware.com/en/VMware-SQL-with-MySQL-for-Tanzu-Application-Service/3.0/mysql-for-tas/use.html#mysql-environment-variables-9
            // - Azure Service Broker: https://docs.vmware.com/en/Tanzu-Cloud-Service-Broker-for-Azure/1.4/csb-azure/GUID-reference-azure-mysql.html#binding-credentials-4
            // - GCP Service Broker: https://docs.vmware.com/en/Tanzu-Cloud-Service-Broker-for-GCP/1.2/csb-gcp/GUID-reference-gcp-mysql.html#binding-credentials-5
            // - AWS Service Broker: https://docs.vmware.com/en/Tanzu-Cloud-Service-Broker-for-AWS/1.5/csb-aws/GUID-reference-aws-mysql.html#binding-credentials-3

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
