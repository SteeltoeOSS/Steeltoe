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

using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.Rabbit.Config;
using Steeltoe.Messaging.Rabbit.Core;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Extensions
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
