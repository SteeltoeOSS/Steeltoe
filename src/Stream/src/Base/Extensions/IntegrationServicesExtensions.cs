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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Integration;
using Steeltoe.Integration.Attributes;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Endpoint;
using Steeltoe.Integration.Handler;
using Steeltoe.Integration.Support;
using Steeltoe.Integration.Support.Converter;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using System;
using System.Linq;
using System.Reflection;

namespace Steeltoe.Stream.Extensions
{
    public static class IntegrationServicesExtensions
    {
        public static IServiceCollection AddIntegrationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IIntegrationServices, IntegrationServices>();

            // Not sure need to be added?
            services.TryAddSingleton<DefaultDatatypeChannelMessageConverter>();

            // services.TryAddSingleton<IMessageConverter>((p) => p.GetRequiredService<DefaultDatatypeChannelMessageConverter>());
            services.TryAddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();

            services.AddSingleton<IMessageChannel, NullChannel>();

            services.AddSingleton<IMessageChannel>((p) =>
           {
               return new PublishSubscribeChannel(p.GetService<IApplicationContext>(), IntegrationContextUtils.ERROR_CHANNEL_BEAN_NAME);
           });
            services.AddSingleton<ILifecycle>((p) =>
           {
               var logger = p.GetRequiredService<ILogger<LoggingHandler>>();
               var handler = new LoggingHandler(p.GetService<IApplicationContext>(), LogLevel.Error, logger);
               var chan = GetRequiredChannel<ISubscribableChannel>(p.GetService<IApplicationContext>(), IntegrationContextUtils.ERROR_CHANNEL_BEAN_NAME);
               return new EventDrivenConsumerEndpoint(p.GetService<IApplicationContext>(), chan, handler);
           });

            // SpringIntegrationProperties
            return services;
        }

        public static IServiceCollection AddServiceActivators<T>(this IServiceCollection services)
            where T : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var targetMethods = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            var added = false;

            foreach (var method in targetMethods)
            {
                var attr = method.GetCustomAttribute<ServiceActivatorAttribute>();
                if (attr != null)
                {
                    services.AddServiceActivator(method, attr);
                    added = true;
                }
            }

            if (added)
            {
                services.TryAddSingleton<T>();
            }

            return services;
        }

        public static IServiceCollection AddServiceActivator(this IServiceCollection services, MethodInfo method, ServiceActivatorAttribute attribute)
        {
            throw new NotImplementedException("AddServiceActivator");

            // StreamListenerMethodValidator streamListenerMethod = new StreamListenerMethodValidator(method);
            // streamListenerMethod.Validate(attribute.Target, attribute.Condition);
            // services.AddSingleton<IStreamListenerMethod>(new StreamListenerMethod(method, attribute));
            // return services;
        }

        // public static IServiceCollection AddServiceActivator(this IServiceCollection services, MethodInfo method, string target, string condition = null, bool copyHeaders = true)
        // {
        //    if (services == null)
        //    {
        //        throw new ArgumentNullException();
        //    }
        //    StreamListenerAttribute attribute = new StreamListenerAttribute(target, condition, copyHeaders);
        //    services.AddStreamListener(method, attribute);
        //    return services;
        // }
        private static T GetRequiredChannel<T>(IApplicationContext context, string name)
            where T : class
        {
            T result = context.GetServices<IMessageChannel>().FirstOrDefault((chan) => chan.ServiceName == name) as T;

            if (result == null)
            {
                throw new InvalidOperationException("Unable to resolve channel:" + name);
            }

            return result;
        }
    }
}
