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

using Steeltoe.CloudFoundry.Connector.Services;

namespace Steeltoe.CloudFoundry.Connector.Hystrix
{
    public class HystrixProviderConfigurer
    {
        internal string Configure(HystrixRabbitServiceInfo si, HystrixProviderConnectorOptions configuration)
        {
            UpdateConfiguration(si, configuration);
            return configuration.ToString();
        }

        internal void UpdateConfiguration(HystrixRabbitServiceInfo si, HystrixProviderConnectorOptions configuration)
        {
            if (si == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(si.Uri))
            {
                if (si.Scheme.Equals(HystrixProviderConnectorOptions.Default_SSLScheme, System.StringComparison.OrdinalIgnoreCase))
                {
                    configuration.SslEnabled = true;
                    configuration.SslPort = si.Port;
                }
                else
                {
                    configuration.Port = si.Port;
                }

                configuration.Username = si.UserName;
                configuration.Password = si.Password;
                configuration.Server = si.Host;
                configuration.VirtualHost = si.Path;
            }
        }
    }
}
