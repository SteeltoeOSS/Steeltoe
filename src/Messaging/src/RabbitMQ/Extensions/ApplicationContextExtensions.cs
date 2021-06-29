// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Core;
using System.Collections.Generic;

namespace Steeltoe.Messaging.RabbitMQ.Extensions
{
    public static class ApplicationContextExtensions
    {
        public static RabbitTemplate GetRabbitTemplate(this IApplicationContext context, string name = null)
        {
            return context.ServiceProvider.GetRabbitTemplate(name);
        }

        public static IRabbitAdmin GetRabbitAdmin(this IApplicationContext context, string name = null)
        {
            return context.ServiceProvider.GetRabbitAdmin(name);
        }

        public static IQueue GetRabbitQueue(this IApplicationContext context, string name)
        {
            return context.ServiceProvider.GetRabbitQueue(name);
        }

        public static Config.IExchange GetRabbitExchange(this IApplicationContext context, string name)
        {
            return context.ServiceProvider.GetRabbitExchange(name);
        }

        public static IBinding GetRabbitBinding(this IApplicationContext context, string name)
        {
            return context.ServiceProvider.GetRabbitBinding(name);
        }

        public static IEnumerable<IQueue> GetRabbitQueues(this IApplicationContext context)
        {
            return context.ServiceProvider.GetRabbitQueues();
        }

        public static IEnumerable<IExchange> GetRabbitExchanges(this IApplicationContext context)
        {
            return context.ServiceProvider.GetRabbitExchanges();
        }

        public static IEnumerable<IBinding> GetRabbitBindings(this IApplicationContext context)
        {
            return context.ServiceProvider.GetRabbitBindings();
        }
    }
}
