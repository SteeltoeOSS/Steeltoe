// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding;

internal sealed class MongoDbPostProcessor : CloudFoundryConfigurationPostProcessor
{
    internal const string BindingType = "mongodb";

    public override void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configurationData)
    {
        if (!provider.IsBindingTypeEnabled(BindingType))
        {
            return;
        }

        foreach (string key in FilterKeys(configurationData, BindingType))
        {
            var mapper = ServiceBindingMapper.Create(configurationData, key, BindingType);

            // See MongoDB connection string parameters at: https://www.mongodb.com/docs/manual/reference/connection-string/
            mapper.MapFromTo("credentials:uri", "url");

            if (mapper.BindingProvider == "csb-azure-mongodb")
            {
                // The MongoDB Azure Service Broker solely provides an url, which contains the authentication database.
                // We extract this and expose it as the database used for application data as well.
                string url = mapper.GetFromValue("credentials:uri");
                var uri = new Uri(url);
                string database = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped);

                mapper.SetToValue("database", database);
            }
        }
    }
}
