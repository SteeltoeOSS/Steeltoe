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
using Steeltoe.Messaging.Rabbit.Config;
using System;

namespace Steeltoe.Messaging.Rabbit.Extensions
{
    public static class RabbitListenerExtensions
    {
        public static IServiceCollection AddRabbitListeners<T>(this IServiceCollection services)
            where T : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var metadata = RabbitListenerMetadata.BuildMetadata(typeof(T));
            if (metadata != null)
            {
                services.AddSingleton(metadata);
                services.AddSingleton<T>();
            }

            return services;
        }
    }
}
