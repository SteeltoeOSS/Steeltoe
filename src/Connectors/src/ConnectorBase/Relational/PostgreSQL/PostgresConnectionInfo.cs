// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.PostgreSql
{
    public class PostgresConnectionInfo : Connection, IConnectionInfo
    {
        public Connection Get(IConfiguration configuration, string serviceName)
        {
            var info = serviceName == null
             ? configuration.GetSingletonServiceInfo<PostgresServiceInfo>()
             : configuration.GetRequiredServiceInfo<PostgresServiceInfo>(serviceName);

            var postgresConfig = new PostgresProviderConnectorOptions(configuration);
            var configurer = new PostgresProviderConfigurer();

            var connectionString = configurer.Configure(info, postgresConfig);

            ClientCertificate = postgresConfig.ClientCertificate;
            ClientKey = postgresConfig.ClientKey;
            SslRootCertificate = postgresConfig.SslRootCertificate;

            ConnectionString = connectionString;
            Name = "Postgres" + serviceName?.Insert(0, "-");

            return this;
        }

        public string ClientCertificate { get; set; }

        public string ClientKey { get; set; }

        public string SslRootCertificate { get; set; }
    }
}