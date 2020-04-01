﻿// Copyright 2017 the original author or authors.
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
using System.Net;

namespace Steeltoe.Connector.RabbitMQ
{
    public class RabbitMQProviderConfigurer
    {
        internal string Configure(RabbitMQServiceInfo si, RabbitMQProviderConnectorOptions configuration)
        {
            UpdateConfiguration(si, configuration);
            return configuration.ToString();
        }

        internal void UpdateConfiguration(RabbitMQServiceInfo si, RabbitMQProviderConnectorOptions configuration)
        {
            if (si == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(si.Uri))
            {
                if (si.Scheme.Equals(RabbitMQProviderConnectorOptions.Default_SSLScheme, System.StringComparison.OrdinalIgnoreCase))
                {
                    configuration.SslEnabled = true;
                    configuration.SslPort = si.Port;
                }
                else
                {
                    configuration.Port = si.Port;
                }

                if (configuration.UrlEncodedCredentials)
                {
                    configuration.Username = WebUtility.UrlDecode(si.UserName);
                    configuration.Password = WebUtility.UrlDecode(si.Password);
                }
                else
                {
                    configuration.Username = si.UserName;
                    configuration.Password = si.Password;
                }

                configuration.Server = si.Host;
                configuration.VirtualHost = si.Path;
            }
        }
    }
}
