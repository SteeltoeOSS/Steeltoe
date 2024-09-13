// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry.ServiceBindings.PostProcessors;

internal sealed class MongoDbCloudFoundryPostProcessor : CloudFoundryPostProcessor
{
    internal const string BindingType = "mongodb";

    public override void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string?> configurationData)
    {
        foreach (string key in FilterKeys(configurationData, BindingType, KeyFilterSources.Tag))
        {
            var mapper = ServiceBindingMapper.Create(configurationData, key, BindingType);

            // Mapping from CloudFoundry service binding credentials to driver-specific connection string parameters.
            // The available credentials are documented at:
            // - Azure Service Broker: https://docs.vmware.com/en/Tanzu-Cloud-Service-Broker-for-Azure/1.4/csb-azure/GUID-reference-azure-cosmosdb-mongo.html#binding-credentials-3

            mapper.MapFromTo("credentials:uri", "url");

            if (mapper.BindingProvider == "csb-azure-mongodb")
            {
                // The MongoDB Azure Service Broker solely provides an url, which contains the authentication database.
                // We extract this and expose it as the database used for application data as well.
                string? url = mapper.GetFromValue("credentials:uri");

                if (!string.IsNullOrEmpty(url))
                {
                    var uri = new Uri(url);
                    string database = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped);

                    mapper.SetToValue("database", database);
                }
            }
        }
    }
}
