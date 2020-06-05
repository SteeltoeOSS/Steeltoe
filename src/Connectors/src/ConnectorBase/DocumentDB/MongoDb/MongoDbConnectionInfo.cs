// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.MongoDb;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector
{
    public class MongoDbConnectionInfo : IConnectionInfo
    {
        public Connection Get(IConfiguration configuration, string serviceName)
        {
            var info = serviceName == null
               ? configuration.GetSingletonServiceInfo<MongoDbServiceInfo>()
               : configuration.GetRequiredServiceInfo<MongoDbServiceInfo>(serviceName);

            var mongoConfig = new MongoDbConnectorOptions(configuration);
            var configurer = new MongoDbProviderConfigurer();
            return new Connection
            {
                ConnectionString = configurer.Configure(info, mongoConfig),
                Name = "MongoDb" + serviceName?.Insert(0, "-")
            };
        }
    }
}