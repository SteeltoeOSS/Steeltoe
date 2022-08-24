// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.MongoDb;

public class MongoDbConnectionInfo : IConnectionInfo
{
    public Connection Get(IConfiguration configuration, string serviceName)
    {
        MongoDbServiceInfo info = string.IsNullOrEmpty(serviceName)
            ? configuration.GetSingletonServiceInfo<MongoDbServiceInfo>()
            : configuration.GetRequiredServiceInfo<MongoDbServiceInfo>(serviceName);

        return GetConnection(info, configuration);
    }

    public Connection Get(IConfiguration configuration, IServiceInfo serviceInfo)
    {
        return GetConnection((MongoDbServiceInfo)serviceInfo, configuration);
    }

    public bool IsSameType(string serviceType)
    {
        return serviceType.Equals("mongodb", StringComparison.InvariantCultureIgnoreCase);
    }

    public bool IsSameType(IServiceInfo serviceInfo)
    {
        return serviceInfo is MongoDbServiceInfo;
    }

    private Connection GetConnection(MongoDbServiceInfo info, IConfiguration configuration)
    {
        var options = new MongoDbConnectorOptions(configuration);
        var configurer = new MongoDbProviderConfigurer();
        return new Connection(configurer.Configure(info, options), "MongoDb", info);
    }
}
