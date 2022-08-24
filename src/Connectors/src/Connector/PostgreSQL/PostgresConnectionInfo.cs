// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.PostgreSql;

public class PostgresConnectionInfo : IConnectionInfo
{
    public Connection Get(IConfiguration configuration, string serviceName)
    {
        PostgresServiceInfo info = string.IsNullOrEmpty(serviceName)
            ? configuration.GetSingletonServiceInfo<PostgresServiceInfo>()
            : configuration.GetRequiredServiceInfo<PostgresServiceInfo>(serviceName);

        return GetConnection(info, configuration);
    }

    public Connection Get(IConfiguration configuration, IServiceInfo serviceInfo)
    {
        return GetConnection((PostgresServiceInfo)serviceInfo, configuration);
    }

    public bool IsSameType(string serviceType)
    {
        return serviceType.Equals("postgres", StringComparison.InvariantCultureIgnoreCase) ||
            serviceType.Equals("postgresql", StringComparison.InvariantCultureIgnoreCase);
    }

    public bool IsSameType(IServiceInfo serviceInfo)
    {
        return serviceInfo is PostgresServiceInfo;
    }

    private Connection GetConnection(PostgresServiceInfo info, IConfiguration configuration)
    {
        var options = new PostgresProviderConnectorOptions(configuration);
        var configurer = new PostgresProviderConfigurer();
        var connection = new Connection(configurer.Configure(info, options), "Postgres", info);
        connection.Properties.Add("ClientCertificate", options.ClientCertificate);
        connection.Properties.Add("ClientKey", options.ClientKey);
        connection.Properties.Add("SslRootCertificate", options.SslRootCertificate);

        return connection;
    }
}
