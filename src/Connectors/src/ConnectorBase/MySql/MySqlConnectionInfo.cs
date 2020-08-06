// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Services;
using System;

namespace Steeltoe.Connector.MySql
{
    public class MySqlConnectionInfo : IConnectionInfo
    {
        public Connection Get(IConfiguration configuration, string serviceName)
        {
            var info = string.IsNullOrEmpty(serviceName)
                ? configuration.GetSingletonServiceInfo<MySqlServiceInfo>()
                : configuration.GetRequiredServiceInfo<MySqlServiceInfo>(serviceName);
            return GetConnection(info, configuration);
        }

        public Connection Get(IConfiguration configuration, IServiceInfo serviceInfo)
            => GetConnection((MySqlServiceInfo)serviceInfo, configuration);

        public bool IsSameType(string serviceType) => serviceType.Equals("mysql", StringComparison.InvariantCultureIgnoreCase);

        public bool IsSameType(IServiceInfo serviceInfo) => serviceInfo is MySqlServiceInfo;

        private Connection GetConnection(MySqlServiceInfo info, IConfiguration configuration)
        {
            var mySqlConfig = new MySqlProviderConnectorOptions(configuration);
            var configurer = new MySqlProviderConfigurer();
            return new Connection(configurer.Configure(info, mySqlConfig), "MySql", info);
        }
    }
}