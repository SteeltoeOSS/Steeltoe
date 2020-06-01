// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.CloudFoundry.Connector.MySql;
using Steeltoe.CloudFoundry.Connector.Services;

namespace Steeltoe.CloudFoundry.Connector.RabbitMQ
{
    public class RabbitMQConnectionInfo : IConnectionInfo
    {
        public Connection Get(IConfiguration configuration, string serviceName)
        {
            var info = serviceName == null
              ? configuration.GetSingletonServiceInfo<RabbitMQServiceInfo>()
              : configuration.GetRequiredServiceInfo<RabbitMQServiceInfo>(serviceName);

            var rabbitConfig = new RabbitMQProviderConnectorOptions(configuration);
            var configurer = new RabbitMQProviderConfigurer();
            return new Connection
            {
                ConnectionString = configurer.Configure(info, rabbitConfig),
                Name = "RabbitMQ" + serviceName?.Insert(0, "-")
            };
        }
    }
}