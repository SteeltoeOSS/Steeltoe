// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CloudFoundry.Connector.Services;
using System.Net;

namespace Steeltoe.CloudFoundry.Connector.MongoDb
{
    public class MongoDbProviderConfigurer
    {
        public string Configure(MongoDbServiceInfo si, MongoDbConnectorOptions configuration)
        {
            UpdateConfiguration(si, configuration);
            return configuration.ToString();
        }

        public void UpdateConfiguration(MongoDbServiceInfo si, MongoDbConnectorOptions configuration)
        {
            if (si == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(si.Uri))
            {
                configuration.Uri = si.Uri;

                // the rest of this is unlikely to really be needed when a uri is available
                // but the properties are here, so let's go ahead and set them just in case
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
                configuration.Database = si.Path;
            }
        }
    }
}
