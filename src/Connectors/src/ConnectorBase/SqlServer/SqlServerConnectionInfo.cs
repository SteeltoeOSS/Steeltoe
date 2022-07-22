// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Services;
using System;

namespace Steeltoe.Connector.SqlServer;

public class SqlServerConnectionInfo : IConnectionInfo
{
    public Connection Get(IConfiguration configuration, string serviceName)
    {
        var info = string.IsNullOrEmpty(serviceName)
            ? configuration.GetSingletonServiceInfo<SqlServerServiceInfo>()
            : configuration.GetRequiredServiceInfo<SqlServerServiceInfo>(serviceName);
        return GetConnection(info, configuration);
    }

    public Connection Get(IConfiguration configuration, IServiceInfo serviceInfo)
        => GetConnection((SqlServerServiceInfo)serviceInfo, configuration);

    public bool IsSameType(string serviceType) =>
        serviceType.Equals("sqlserver", StringComparison.InvariantCultureIgnoreCase) ||
        serviceType.Equals("mssql", StringComparison.InvariantCultureIgnoreCase);

    public bool IsSameType(IServiceInfo serviceInfo) => serviceInfo is SqlServerServiceInfo;

    private Connection GetConnection(SqlServerServiceInfo info, IConfiguration configuration)
    {
        var sqlConfig = new SqlServerProviderConnectorOptions(configuration);
        var configurer = new SqlServerProviderConfigurer();
        return new Connection(configurer.Configure(info, sqlConfig), "SqlServer", info);
    }
}