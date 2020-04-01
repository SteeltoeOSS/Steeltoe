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

using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.CosmosDb
{
    public class CosmosDbProviderConfigurer
    {
        public string Configure(CosmosDbServiceInfo si, CosmosDbConnectorOptions configuration)
        {
            UpdateConfiguration(si, configuration);
            return configuration.ToString();
        }

        public void UpdateConfiguration(CosmosDbServiceInfo si, CosmosDbConnectorOptions configuration)
        {
            if (si == null)
            {
                return;
            }

            configuration.Host = si.Host;
            configuration.MasterKey = si.MasterKey;
            configuration.ReadOnlyKey = si.ReadOnlyKey;
            configuration.DatabaseId = si.DatabaseId;
            configuration.DatabaseLink = si.DatabaseLink;
        }
    }
}
