// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.CloudFoundry.Connector.Services;

namespace Steeltoe.CloudFoundry.Connector.SqlServer
{
    public class SqlServerConnectionInfo : IConnectionInfo
    {
        public Connection Get(IConfiguration configuration, string serviceName)
        {
            var info = serviceName == null
            ? configuration.GetSingletonServiceInfo<SqlServerServiceInfo>()
            : configuration.GetRequiredServiceInfo<SqlServerServiceInfo>(serviceName);

            var sqlConfig = new SqlServerProviderConnectorOptions(configuration);
            var configurer = new SqlServerProviderConfigurer();
            return new Connection
            {
                ConnectionString = configurer.Configure(info, sqlConfig),
                Name = "SqlServer" + serviceName?.Insert(0, "-")
            };
        }
    }
}