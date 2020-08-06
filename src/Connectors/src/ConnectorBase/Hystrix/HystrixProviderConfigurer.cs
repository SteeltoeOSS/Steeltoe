﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.Hystrix
{
    public class HystrixProviderConfigurer
    {
        internal string Configure(HystrixRabbitMQServiceInfo si, HystrixProviderConnectorOptions configuration)
        {
            UpdateConfiguration(si, configuration);
            return configuration.ToString();
        }

        internal void UpdateConfiguration(HystrixRabbitMQServiceInfo si, HystrixProviderConnectorOptions configuration)
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
