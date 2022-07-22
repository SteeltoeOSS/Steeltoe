// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Messaging.RabbitMQ.Extensions;

namespace Steeltoe.Messaging.RabbitMQ;

public abstract class AbstractTest
{
    protected virtual ServiceCollection CreateContainer(ConfigurationBuilder configurationBuilder = null)
    {
        var services = new ServiceCollection();
        if (configurationBuilder == null)
        {
            configurationBuilder = new ConfigurationBuilder();
        }

        var configuration = configurationBuilder.Build();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddRabbitHostingServices();
        return services;
    }
}