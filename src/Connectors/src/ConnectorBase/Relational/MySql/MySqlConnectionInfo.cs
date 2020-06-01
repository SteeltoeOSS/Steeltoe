// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.CloudFoundry.Connector.MySql;
using Steeltoe.CloudFoundry.Connector.Services;

namespace Steeltoe.CloudFoundry.Connector
{
    public class MySqlConnectionInfo : IConnectionInfo
    {
        public Connection Get(IConfiguration configuration, string serviceName)
        {
            var info = serviceName == null
              ? configuration.GetSingletonServiceInfo<MySqlServiceInfo>()
              : configuration.GetRequiredServiceInfo<MySqlServiceInfo>(serviceName);

            var mySqlConfig = new MySqlProviderConnectorOptions(configuration);
            var configurer = new MySqlProviderConfigurer();
            var connString = configurer.Configure(info, mySqlConfig);
            return new Connection
            {
                ConnectionString = connString,
                Name = "MySql" + serviceName?.Insert(0, "-")
            };
        }
    }
}