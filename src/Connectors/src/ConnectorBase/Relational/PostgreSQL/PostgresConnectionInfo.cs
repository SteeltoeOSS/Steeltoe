// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.PostgreSql
{
    public class PostgresConnectionInfo : Connection, IConnectionInfo, IConnectionServiceInfo
    {
        public Connection Get(IConfiguration configuration, string serviceName)
        {
            var info = serviceName == null
             ? configuration.GetSingletonServiceInfo<PostgresServiceInfo>()
             : configuration.GetRequiredServiceInfo<PostgresServiceInfo>(serviceName);
            return GetConnection(info, configuration);
        }

        public Connection Get(IConfiguration configuration, IServiceInfo serviceInfo)
        {
            return GetConnection((PostgresServiceInfo)serviceInfo, configuration);
        }

        public string ClientCertificate { get; set; }

        public string ClientKey { get; set; }

        public string SslRootCertificate { get; set; }

        private Connection GetConnection(PostgresServiceInfo info, IConfiguration configuration)
        {
            var postgresConfig = new PostgresProviderConnectorOptions(configuration);
            var configurer = new PostgresProviderConfigurer();

            var connectionString = configurer.Configure(info, postgresConfig);

            ClientCertificate = postgresConfig.ClientCertificate;
            ClientKey = postgresConfig.ClientKey;
            SslRootCertificate = postgresConfig.SslRootCertificate;

            ConnectionString = connectionString;
            Name = "Postgres" + info?.Id?.Insert(0, "-");

            return this;
        }
    }
}