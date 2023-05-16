// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connectors.Services;

namespace Steeltoe.Connectors.PostgreSql;

public class PostgreSqlConnectionInfo : IConnectionInfo
{
    public Connection Get(IConfiguration configuration, string serviceName)
    {
        PostgreSqlServiceInfo info = string.IsNullOrEmpty(serviceName)
            ? configuration.GetSingletonServiceInfo<PostgreSqlServiceInfo>()
            : configuration.GetRequiredServiceInfo<PostgreSqlServiceInfo>(serviceName);

        return GetConnection(info, configuration);
    }

    public Connection Get(IConfiguration configuration, IServiceInfo serviceInfo)
    {
        return GetConnection((PostgreSqlServiceInfo)serviceInfo, configuration);
    }

    public bool IsSameType(string serviceType)
    {
        return serviceType.Equals("postgres", StringComparison.OrdinalIgnoreCase) || serviceType.Equals("postgresql", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsSameType(IServiceInfo serviceInfo)
    {
        return serviceInfo is PostgreSqlServiceInfo;
    }

    private Connection GetConnection(PostgreSqlServiceInfo info, IConfiguration configuration)
    {
        var options = new PostgreSqlProviderConnectorOptions(configuration);
        var configurer = new PostgreSqlProviderConfigurer();
        var connection = new Connection(configurer.Configure(info, options), "Postgres", info);
        connection.Properties.Add("ClientCertificate", options.ClientCertificate);
        connection.Properties.Add("ClientKey", options.ClientKey);
        connection.Properties.Add("SslRootCertificate", options.SslRootCertificate);

        return connection;
    }
}
