// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.CosmosDb
{
    public class CosmosDbReadOnlyConnectionInfo : IConnectionInfo
    {
        public Connection Get(IConfiguration configuration, string serviceName)
        {
            var info = serviceName == null
               ? configuration.GetSingletonServiceInfo<CosmosDbServiceInfo>()
               : configuration.GetRequiredServiceInfo<CosmosDbServiceInfo>(serviceName);

            var cosmosConfig = new CosmosDbConnectorOptions(configuration)
            {
                UseReadOnlyCredentials = true
            };

            var configurer = new CosmosDbProviderConfigurer();
            return new Connection
            {
                ConnectionString = configurer.Configure(info, cosmosConfig),
                Name = "CosmosDbReadOnly" + serviceName?.Insert(0, "-")
            };
        }
    }
}
