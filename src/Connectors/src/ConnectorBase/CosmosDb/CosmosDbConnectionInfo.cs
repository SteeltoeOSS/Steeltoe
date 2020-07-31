// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.CosmosDb
{
    public class CosmosDbConnectionInfo : IConnectionInfo, IConnectionServiceInfo
    {
        public Connection Get(IConfiguration configuration, string serviceName)
        {
            var info = serviceName == null
               ? configuration.GetSingletonServiceInfo<CosmosDbServiceInfo>()
               : configuration.GetRequiredServiceInfo<CosmosDbServiceInfo>(serviceName);
            return GetConnection(info, configuration);
        }

        public Connection Get(IConfiguration configuration, IServiceInfo serviceInfo)
        {
            return GetConnection((CosmosDbServiceInfo)serviceInfo, configuration);
        }

        private Connection GetConnection(CosmosDbServiceInfo info, IConfiguration configuration)
        {
            var cosmosConfig = new CosmosDbConnectorOptions(configuration);
            var configurer = new CosmosDbProviderConfigurer();
            var conn = new Connection
            {
                ConnectionString = configurer.Configure(info, cosmosConfig),
                Name = "CosmosDb" + info.Id?.Insert(0, "-")
            };
            conn.Properties.Add("DatabaseId", cosmosConfig.DatabaseId);
            conn.Properties.Add("DatabaseLink", cosmosConfig.DatabaseLink);

            return conn;
        }
    }
}
