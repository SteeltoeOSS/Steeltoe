﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Services;
using System;

namespace Steeltoe.Connector.CosmosDb
{
    public class CosmosDbConnectionInfo : IConnectionInfo
    {
        public Connection Get(IConfiguration configuration, string serviceName)
        {
            var info = string.IsNullOrEmpty(serviceName)
                ? configuration.GetSingletonServiceInfo<CosmosDbServiceInfo>()
                : configuration.GetRequiredServiceInfo<CosmosDbServiceInfo>(serviceName);
            return GetConnection(info, configuration);
        }

        public Connection Get(IConfiguration configuration, IServiceInfo serviceInfo)
            => GetConnection((CosmosDbServiceInfo)serviceInfo, configuration);

        public bool IsSameType(string serviceType)
            => serviceType.Equals("cosmosdb", StringComparison.InvariantCultureIgnoreCase);

        public bool IsSameType(IServiceInfo serviceInfo)
            => serviceInfo is CosmosDbServiceInfo;

        private Connection GetConnection(CosmosDbServiceInfo info, IConfiguration configuration)
        {
            var cosmosConfig = new CosmosDbConnectorOptions(configuration);
            var configurer = new CosmosDbProviderConfigurer();
            var conn = new Connection(configurer.Configure(info, cosmosConfig), "CosmosDb", info);
            conn.Properties.Add("DatabaseId", cosmosConfig.DatabaseId);
            conn.Properties.Add("DatabaseLink", cosmosConfig.DatabaseLink);

            return conn;
        }
    }
}
