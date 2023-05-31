// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding.PostProcessors;

internal sealed class CosmosDbCloudFoundryPostProcessor : CloudFoundryPostProcessor
{
    internal const string BindingType = "cosmosdb";

    public override void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configurationData)
    {
        foreach (string key in FilterKeys(configurationData, BindingType))
        {
            var mapper = ServiceBindingMapper.Create(configurationData, key, BindingType);

            // Mapping from CloudFoundry service binding credentials to driver-specific connection string parameters.
            // The available credentials are documented at:
            // - Azure Service Broker: https://docs.vmware.com/en/Tanzu-Cloud-Service-Broker-for-Azure/1.4/csb-azure/GUID-reference-azure-cosmosdb-sql.html#binding-credentials-3

            mapper.MapFromTo("credentials:cosmosdb_host_endpoint", "accountEndpoint");
            mapper.MapFromTo("credentials:cosmosdb_master_key", "accountKey");
            mapper.MapFromTo("credentials:cosmosdb_database_id", "database");
        }
    }
}
