// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CloudFoundry.Connector.Services;

namespace Steeltoe.CloudFoundry.Connector.CosmosDb
{
    public class CosmosDbProviderConfigurer
    {
        public string Configure(CosmosDbServiceInfo si, CosmosDbConnectorOptions configuration)
        {
            UpdateConfiguration(si, configuration);
            return configuration.ToString();
        }

        public void UpdateConfiguration(CosmosDbServiceInfo si, CosmosDbConnectorOptions configuration)
        {
            if (si == null)
            {
                return;
            }

            configuration.Host = si.Host;
            configuration.MasterKey = si.MasterKey;
            configuration.ReadOnlyKey = si.ReadOnlyKey;
            configuration.DatabaseId = si.DatabaseId;
            configuration.DatabaseLink = si.DatabaseLink;
        }
    }
}
