// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.SqlServer;

public class SqlServerConnectionInfo : IConnectionInfo
{
    public Connection Get(IConfiguration configuration, string serviceName)
    {
        SqlServerServiceInfo info = string.IsNullOrEmpty(serviceName)
            ? configuration.GetSingletonServiceInfo<SqlServerServiceInfo>()
            : configuration.GetRequiredServiceInfo<SqlServerServiceInfo>(serviceName);

        return GetConnection(info, configuration);
    }

    public Connection Get(IConfiguration configuration, IServiceInfo serviceInfo)
    {
        return GetConnection((SqlServerServiceInfo)serviceInfo, configuration);
    }

    public bool IsSameType(string serviceType)
    {
        return serviceType.Equals("sqlserver", StringComparison.OrdinalIgnoreCase) || serviceType.Equals("mssql", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsSameType(IServiceInfo serviceInfo)
    {
        return serviceInfo is SqlServerServiceInfo;
    }

    private Connection GetConnection(SqlServerServiceInfo info, IConfiguration configuration)
    {
        var options = new SqlServerProviderConnectorOptions(configuration);
        var configurer = new SqlServerProviderConfigurer();
        return new Connection(configurer.Configure(info, options), "SqlServer", info);
    }
}
