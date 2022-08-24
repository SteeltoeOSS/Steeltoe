// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.MySql;

public class MySqlConnectionInfo : IConnectionInfo
{
    public Connection Get(IConfiguration configuration, string serviceName)
    {
        MySqlServiceInfo info = string.IsNullOrEmpty(serviceName)
            ? configuration.GetSingletonServiceInfo<MySqlServiceInfo>()
            : configuration.GetRequiredServiceInfo<MySqlServiceInfo>(serviceName);

        return GetConnection(info, configuration);
    }

    public Connection Get(IConfiguration configuration, IServiceInfo serviceInfo)
    {
        return GetConnection((MySqlServiceInfo)serviceInfo, configuration);
    }

    public bool IsSameType(string serviceType)
    {
        return serviceType.Equals("mysql", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsSameType(IServiceInfo serviceInfo)
    {
        return serviceInfo is MySqlServiceInfo;
    }

    private Connection GetConnection(MySqlServiceInfo info, IConfiguration configuration)
    {
        var options = new MySqlProviderConnectorOptions(configuration);
        var configurer = new MySqlProviderConfigurer();
        return new Connection(configurer.Configure(info, options), "MySql", info);
    }
}
