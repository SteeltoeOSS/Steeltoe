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
using Steeltoe.CloudFoundry.Connector.Services;

namespace Steeltoe.CloudFoundry.Connector.PostgreSql
{
    public class PostgresConnectionInfo : IConnectionInfo
    {
        public Connection Get(IConfiguration configuration, string serviceName)
        {
            var info = serviceName == null
             ? configuration.GetSingletonServiceInfo<PostgresServiceInfo>()
             : configuration.GetRequiredServiceInfo<PostgresServiceInfo>(serviceName);

            var postgresConfig = new PostgresProviderConnectorOptions(configuration);
            var configurer = new PostgresProviderConfigurer();
            return new Connection
            {
                ConnectionString = configurer.Configure(info, postgresConfig),
                Name = "Postgres" + serviceName?.Insert(0, "-")
            };
        }
    }
}