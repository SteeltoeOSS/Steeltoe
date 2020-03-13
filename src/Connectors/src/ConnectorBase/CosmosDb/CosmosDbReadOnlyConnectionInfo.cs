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

namespace Steeltoe.Connector.CosmosDb
{
    public class CosmosDbReadOnlyConnectionInfo : IConnectionInfo
    {
        public Connection Get(IConfiguration configuration, string serviceName)
        {
            var info = serviceName == null
               ? configuration.GetSingletonServiceInfo<CosmosDbServiceInfo>()
               : configuration.GetRequiredServiceInfo<CosmosDbServiceInfo>(serviceName);

            var cosmosConfig = new CosmosDbConnectorOptions(configuration)
            {
                UseReadOnlyCredentials = true
            };

            var configurer = new CosmosDbProviderConfigurer();
            return new Connection
            {
                ConnectionString = configurer.Configure(info, cosmosConfig),
                Name = "CosmosDbReadOnly" + serviceName?.Insert(0, "-")
            };
        }
    }
}
