// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.CosmosDb;

public class CosmosDbReadOnlyConnectionInfo : IConnectionInfo
{
    public Connection Get(IConfiguration configuration, string serviceName)
    {
        CosmosDbServiceInfo info = string.IsNullOrEmpty(serviceName)
            ? configuration.GetSingletonServiceInfo<CosmosDbServiceInfo>()
            : configuration.GetRequiredServiceInfo<CosmosDbServiceInfo>(serviceName);

        return GetConnection(info, configuration);
    }

    public Connection Get(IConfiguration configuration, IServiceInfo serviceInfo)
    {
        return GetConnection((CosmosDbServiceInfo)serviceInfo, configuration);
    }

    public bool IsSameType(string serviceType)
    {
        return serviceType.Equals("cosmosdb-readonly", StringComparison.InvariantCultureIgnoreCase);
    }

    public bool IsSameType(IServiceInfo serviceInfo)
    {
        return serviceInfo is CosmosDbServiceInfo && serviceInfo.Id.Contains("readonly");
    }

    private Connection GetConnection(CosmosDbServiceInfo info, IConfiguration configuration)
    {
        var options = new CosmosDbConnectorOptions(configuration)
        {
            UseReadOnlyCredentials = true
        };

        var configurer = new CosmosDbProviderConfigurer();

        var conn = new Connection(configurer.Configure(info, options), "CosmosDb-ReadOnly", info);
        conn.Properties.Add("DatabaseId", options.DatabaseId);
        conn.Properties.Add("DatabaseLink", options.DatabaseLink);

        return conn;
    }
}
