// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.Rabbit.Config;
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Messaging.Rabbit.Extensions
{
    public static class ServiceProviderExtensions
    {
        public static RabbitTemplate GetRabbitTemplate(this IServiceProvider provider, string name = null)
        {
            var serviceName = name ?? RabbitTemplate.DEFAULT_SERVICE_NAME;
            return provider.GetServices<RabbitTemplate>().SingleOrDefault((t) => t.ServiceName == serviceName);
        }

        public static RabbitAdmin GetRabbitAdmin(this IServiceProvider provider, string name = null)
        {
            var serviceName = name ?? RabbitAdmin.DEFAULT_SERVICE_NAME;
            return provider.GetServices<RabbitAdmin>().SingleOrDefault((t) => t.ServiceName == serviceName);
        }

        public static IQueue GetRabbitQueue(this IServiceProvider provider, string name)
        {
            return provider.GetServices<IQueue>().SingleOrDefault((t) => t.ServiceName == name);
        }

        public static Config.IExchange GetRabbitExchange(this IServiceProvider provider, string name)
        {
            return provider.GetServices<Config.IExchange>().SingleOrDefault((t) => t.ServiceName == name);
        }

        public static IBinding GetRabbitBinding(this IServiceProvider provider, string name)
        {
            return provider.GetServices<IBinding>().SingleOrDefault((t) => t.ServiceName == name);
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
            var serviceName = factoryName ?? CachingConnectionFactory.DEFAULT_SERVICE_NAME;
            return provider.GetServices<IConnectionFactory>().SingleOrDefault((t) => t.ServiceName == serviceName);
        }
    }
}
