// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.MySql
{
    public class MySqlConnectionInfo : IConnectionInfo, IConnectionServiceInfo
    {
        public Connection Get(IConfiguration configuration, string serviceName)
        {
            var info = serviceName == null
              ? configuration.GetSingletonServiceInfo<MySqlServiceInfo>()
              : configuration.GetRequiredServiceInfo<MySqlServiceInfo>(serviceName);
            return GetConnection(info, configuration);
        }

        public Connection Get(IConfiguration configuration, IServiceInfo serviceInfo)
        {
            return GetConnection((MySqlServiceInfo)serviceInfo, configuration);
        }

        private Connection GetConnection(MySqlServiceInfo info, IConfiguration configuration)
        {
            var mySqlConfig = new MySqlProviderConnectorOptions(configuration);
            var configurer = new MySqlProviderConfigurer();
            var connString = configurer.Configure(info, mySqlConfig);
            return new Connection
            {
                ConnectionString = connString,
                Name = "MySql" + info?.Id?.Insert(0, "-")
            };
        }
    }
}