// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding;

internal sealed class CosmosDbPostProcessor : CloudFoundryConfigurationPostProcessor
{
    internal const string BindingType = "cosmosdb";

    public override void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configurationData)
    {
        if (!provider.IsBindingTypeEnabled(BindingType))
        {
            return;
        }

        foreach (string key in FilterKeys(configurationData, BindingType))
        {
            var mapper = ServiceBindingMapper.Create(configurationData, key, BindingType);

            mapper.MapFromTo("credentials:cosmosdb_host_endpoint", "accountEndpoint");
            mapper.MapFromTo("credentials:cosmosdb_master_key", "accountKey");
            mapper.MapFromTo("credentials:cosmosdb_database_id", "database");
        }
    }
}
