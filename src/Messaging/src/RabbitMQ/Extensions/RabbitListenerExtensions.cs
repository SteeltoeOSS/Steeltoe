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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Messaging.Rabbit.Config;
using System;

namespace Steeltoe.Messaging.Rabbit.Extensions
{
    public static class RabbitListenerExtensions
    {
        public static IServiceCollection AddRabbitListeners(this IServiceCollection services, IConfiguration config, params Type[] listenerServices)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            foreach (var t in listenerServices)
            {
                var metadata = RabbitListenerMetadata.BuildMetadata(services, t);
                if (metadata != null)
                {
                    services.AddSingleton(metadata);
                }

                RabbitListenerDeclareAtrributeProcessor.ProcessDeclareAttributes(services, config, t);
            }

            return services;
        }

        public static IServiceCollection AddRabbitListeners<T>(this IServiceCollection services, IConfiguration config = null)
            where T : class
        {
            return services.AddRabbitListeners(config, typeof(T));
        }
    }
}
