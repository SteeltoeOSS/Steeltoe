// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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