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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Stream.Attributes;
using Steeltoe.Stream.Binding;
using Steeltoe.Stream.Config;
using System;
using System.Reflection;

namespace Steeltoe.Stream.Extensions
{
    public static class StreamListenerExtensions
    {
        public static IServiceCollection AddStreamListeners<T>(this IServiceCollection services)
            where T : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var targetMethods = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            var listenersAdded = false;

            foreach (var method in targetMethods)
            {
                var attr = method.GetCustomAttribute<StreamListenerAttribute>();
                if (attr != null)
                {
                    services.AddStreamListener(method, attr);
                    listenersAdded = true;
                }
            }

            if (listenersAdded)
            {
                services.TryAddSingleton<T>();
            }

            return services;
        }

        public static IServiceCollection AddStreamListener(this IServiceCollection services, MethodInfo method, StreamListenerAttribute attribute)
        {
            var streamListenerMethod = new StreamListenerMethodValidator(method);
            streamListenerMethod.Validate(attribute.Target, attribute.Condition);

            services.AddSingleton<IStreamListenerMethod>(new StreamListenerMethod(method, attribute));
            return services;
        }

        public static IServiceCollection AddStreamListener(this IServiceCollection services, MethodInfo method, string target, string condition = null, bool copyHeaders = true)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var attribute = new StreamListenerAttribute(target, condition, copyHeaders);
            services.AddStreamListener(method, attribute);
            return services;
        }
    }
}
