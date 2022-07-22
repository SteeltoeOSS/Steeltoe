// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Services;
using System;

namespace Steeltoe.Connector.RabbitMQ;

public class RabbitMQConnectionInfo : IConnectionInfo
{
    public Connection Get(IConfiguration configuration, string serviceName)
    {
        var info = string.IsNullOrEmpty(serviceName)
            ? configuration.GetSingletonServiceInfo<RabbitMQServiceInfo>()
            : configuration.GetRequiredServiceInfo<RabbitMQServiceInfo>(serviceName);
        return GetConnection(info, configuration);
    }

    public Connection Get(IConfiguration configuration, IServiceInfo serviceInfo)
        => GetConnection((RabbitMQServiceInfo)serviceInfo, configuration);

    public bool IsSameType(string serviceType) => serviceType.Equals("rabbitmq", StringComparison.InvariantCultureIgnoreCase);

    public bool IsSameType(IServiceInfo serviceInfo) => serviceInfo is RabbitMQServiceInfo;

    private Connection GetConnection(RabbitMQServiceInfo info, IConfiguration configuration)
    {
        var rabbitConfig = new RabbitMQProviderConnectorOptions(configuration);
        var configurer = new RabbitMQProviderConfigurer();
        return new Connection(configurer.Configure(info, rabbitConfig), "RabbitMQ", info);
    }
}