// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.Oracle
{
    public class OracleConnectionInfo : IConnectionInfo, IConnectionServiceInfo
    {
        public Connection Get(IConfiguration configuration, string serviceName)
        {
            var info = serviceName == null
                ? configuration.GetSingletonServiceInfo<OracleServiceInfo>()
                : configuration.GetRequiredServiceInfo<OracleServiceInfo>(serviceName);
            return GetConnection(info, configuration);
        }

        public Connection Get(IConfiguration configuration, IServiceInfo serviceInfo)
        {
            return GetConnection((OracleServiceInfo)serviceInfo, configuration);
        }

        private Connection GetConnection(OracleServiceInfo info, IConfiguration configuration)
        {
            var oracleConfig = new OracleProviderConnectorOptions(configuration);
            var configurer = new OracleProviderConfigurer();
            var connString = configurer.Configure(info, oracleConfig).ToString();
            return new Connection
            {
                ConnectionString = connString,
                Name = "Oracle" + info?.Id?.Insert(0, "-")
            };
        }
    }
}
