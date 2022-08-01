// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;

namespace Steeltoe.Messaging.RabbitMQ.Extensions;

public static class ServiceProviderExtensions
{
    public static RabbitTemplate GetRabbitTemplate(this IServiceProvider provider, string name = null)
    {
        var serviceName = name ?? RabbitTemplate.DefaultServiceName;
        return provider.GetServices<RabbitTemplate>().SingleOrDefault(t => t.ServiceName == serviceName);
    }

    public static RabbitAdmin GetRabbitAdmin(this IServiceProvider provider, string name = null)
    {
        var serviceName = name ?? RabbitAdmin.DefaultServiceName;
        return provider.GetServices<RabbitAdmin>().SingleOrDefault(t => t.ServiceName == serviceName);
    }

    public static IQueue GetRabbitQueue(this IServiceProvider provider, string name)
    {
        return provider.GetServices<IQueue>().SingleOrDefault(t => t.ServiceName == name);
    }

    public static IExchange GetRabbitExchange(this IServiceProvider provider, string name)
    {
        return provider.GetServices<IExchange>().SingleOrDefault(t => t.ServiceName == name);
    }

    public static IBinding GetRabbitBinding(this IServiceProvider provider, string name)
    {
        return provider.GetServices<IBinding>().SingleOrDefault(t => t.ServiceName == name);
    }

    public static IEnumerable<IQueue> GetRabbitQueues(this IServiceProvider provider)
    {
        return provider.GetServices<IQueue>();
    }

    public static IEnumerable<IExchange> GetRabbitExchanges(this IServiceProvider provider)
    {
        return provider.GetServices<IExchange>();
    }

    public static IEnumerable<IBinding> GetRabbitBindings(this IServiceProvider provider)
    {
        return provider.GetServices<IBinding>();
    }

    public static IApplicationContext GetApplicationContext(this IServiceProvider provider)
    {
        return provider.GetService<IApplicationContext>();
    }

    public static IConnectionFactory GetRabbitConnectionFactory(this IServiceProvider provider, string factoryName = null)
    {
        var serviceName = factoryName ?? CachingConnectionFactory.DefaultServiceName;
        return provider.GetServices<IConnectionFactory>().SingleOrDefault(t => t.ServiceName == serviceName);
    }
}
