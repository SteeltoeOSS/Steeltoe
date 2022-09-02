// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Messaging.RabbitMQ.Extensions;

namespace Steeltoe.Messaging.RabbitMQ;

public abstract class AbstractTest
{
    protected virtual ServiceCollection CreateContainer()
    {
        var services = new ServiceCollection();
        var configurationBuilder = new ConfigurationBuilder();

        IConfigurationRoot configuration = configurationBuilder.Build();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddRabbitHostingServices();
        return services;
    }
}
