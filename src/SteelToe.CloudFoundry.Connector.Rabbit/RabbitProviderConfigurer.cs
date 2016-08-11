//
// Copyright 2015 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//


using SteelToe.CloudFoundry.Connector.Services;

namespace SteelToe.CloudFoundry.Connector.Rabbit
{
    public class RabbitProviderConfigurer
    {
        internal string Configure(RabbitServiceInfo si, RabbitProviderConnectorOptions configuration)
        {
            UpdateConfiguration(si, configuration);
            return configuration.ToString();
        }

        internal void UpdateConfiguration(RabbitServiceInfo si, RabbitProviderConnectorOptions configuration)
        {
            if (si == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(si.Uri) )
            {
                configuration.Port = si.Port;
                configuration.Username = si.UserName;
                configuration.Password = si.Password;
                configuration.Server = si.Host;
                configuration.VirtualHost = si.Path;
            }
        }
    }
}
