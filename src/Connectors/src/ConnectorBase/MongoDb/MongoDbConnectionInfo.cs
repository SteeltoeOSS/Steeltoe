// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Services;
using System;

namespace Steeltoe.Connector.MongoDb;

public class MongoDbConnectionInfo : IConnectionInfo
{
    public Connection Get(IConfiguration configuration, string serviceName)
    {
        var info = string.IsNullOrEmpty(serviceName)
            ? configuration.GetSingletonServiceInfo<MongoDbServiceInfo>()
            : configuration.GetRequiredServiceInfo<MongoDbServiceInfo>(serviceName);
        return GetConnection(info, configuration);
    }

    public Connection Get(IConfiguration configuration, IServiceInfo serviceInfo)
        => GetConnection((MongoDbServiceInfo)serviceInfo, configuration);

    public bool IsSameType(string serviceType) => serviceType.Equals("mongodb", StringComparison.InvariantCultureIgnoreCase);

    public bool IsSameType(IServiceInfo serviceInfo) => serviceInfo is MongoDbServiceInfo;

    private Connection GetConnection(MongoDbServiceInfo info, IConfiguration configuration)
    {
        var mongoConfig = new MongoDbConnectorOptions(configuration);
        var configurer = new MongoDbProviderConfigurer();
        return new Connection(configurer.Configure(info, mongoConfig), "MongoDb", info);
    }
}
