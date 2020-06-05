// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connector.Services;
using System.Net;

namespace Steeltoe.Connector.Oracle
{
    public class OracleProviderConfigurer
    {
        public string Configure(OracleServiceInfo si, OracleProviderConnectorOptions configuration)
        {
            UpdateConfiguration(si, configuration);
            return configuration.ToString();
        }

        public void UpdateConfiguration(OracleServiceInfo si, OracleProviderConnectorOptions configuration)
        {
            if (si == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(si.Uri))
            {
                configuration.Port = si.Port;
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
                configuration.ServiceName = si.Path;
            }
        }
    }
}
