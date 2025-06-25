// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry.ServiceBindings.PostProcessors;

internal sealed class SqlServerCloudFoundryPostProcessor : CloudFoundryPostProcessor
{
    internal const string BindingType = "sqlserver";

    public override void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string?> configurationData)
    {
        foreach (string key in FilterKeys(configurationData, BindingType, KeyFilterSources.Tag))
        {
            var mapper = ServiceBindingMapper.Create(configurationData, key, BindingType);

            // Mapping from CloudFoundry service binding credentials to driver-specific connection string parameters.
            // The available credentials are documented at:
            // - Azure Service Broker: https://techdocs.broadcom.com/us/en/vmware-tanzu/platform-services/tanzu-cloud-service-broker-for-microsoft-azure/1-13/csb-azure/reference-azure-mssql-db.html#binding-creds
            // - AWS Service Broker: https://techdocs.broadcom.com/us/en/vmware-tanzu/platform-services/tanzu-cloud-service-broker-for-aws/1-14/csb-aws/reference-aws-mssql.html#binding-creds

            mapper.MapFromTo("credentials:hostname", "Data Source");
            mapper.MapFromAppendTo("credentials:port", "Data Source", ",");
            mapper.MapFromTo("credentials:name", "Initial Catalog");
            mapper.MapFromTo("credentials:username", "User ID");
            mapper.MapFromTo("credentials:password", "Password");
        }
    }
}
