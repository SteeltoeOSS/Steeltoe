// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Attributes;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Config;
using Steeltoe.Integration.Endpoint;
using Steeltoe.Integration.Handler;
using Steeltoe.Integration.Support;
using Steeltoe.Integration.Support.Converter;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using System;
using System.Linq;
using System.Reflection;

namespace Steeltoe.Integration.Extensions
{
    public static class IntegrationServicesExtensions
    {
        public static IServiceCollection AddErrorChannel(this IServiceCollection services)
        {
            services.RegisterService(IntegrationContextUtils.ERROR_CHANNEL_BEAN_NAME, typeof(IMessageChannel));
            services.AddSingleton<IMessageChannel>((p) =>
            {
                var context = p.GetService<IApplicationContext>();
                return new PublishSubscribeChannel(context, IntegrationContextUtils.ERROR_CHANNEL_BEAN_NAME);
            });

            services.RegisterService(IntegrationContextUtils.ERROR_CHANNEL_BEAN_NAME, typeof(ISubscribableChannel));
            services.AddSingleton((p) =>
            {
                var context = p.GetService<IApplicationContext>();
                return GetRequiredChannel<ISubscribableChannel>(context, IntegrationContextUtils.ERROR_CHANNEL_BEAN_NAME);
            });

            return services;
        }

        public static IServiceCollection AddNullChannel(this IServiceCollection services)
        {
            services.RegisterService(IntegrationContextUtils.NULL_CHANNEL_BEAN_NAME, typeof(IMessageChannel));
            services.AddSingleton<IMessageChannel, NullChannel>();
            services.RegisterService(IntegrationContextUtils.NULL_CHANNEL_BEAN_NAME, typeof(IPollableChannel));
            services.AddSingleton((p) =>
            {
                var context = p.GetService<IApplicationContext>();
                return GetRequiredChannel<IPollableChannel>(context, IntegrationContextUtils.NULL_CHANNEL_BEAN_NAME);
            });

            return services;
        }

        public static IServiceCollection AddQueueChannel(this IServiceCollection services, string channelName)
        {
            return services.AddQueueChannel(channelName, null);
        }

        public static IServiceCollection AddQueueChannel(this IServiceCollection services, string channelName, Action<IServiceProvider, QueueChannel> configure)
        {
            if (string.IsNullOrEmpty(channelName))
            {
                throw new ArgumentNullException(nameof(channelName));
            }

            services.RegisterService(channelName, typeof(IMessageChannel));
            services.AddSingleton<IMessageChannel>((p) =>
            {
                var context = p.GetService<IApplicationContext>();
                var chan = new QueueChannel(context)
                {
                    ServiceName = channelName
                };

                configure?.Invoke(p, chan);

                return chan;
            });

            services.RegisterService(channelName, typeof(IPollableChannel));
            services.AddSingleton((p) =>
            {
                var context = p.GetService<IApplicationContext>();
                return GetRequiredChannel<IPollableChannel>(context, channelName);
            });

            return services;
        }

        public static IServiceCollection AddDirectChannel(this IServiceCollection services, string channelName)
        {
            return services.AddDirectChannel(channelName, null);
        }

        public static IServiceCollection AddDirectChannel(this IServiceCollection services, string channelName, Action<IServiceProvider, DirectChannel> configure)
        {
            if (string.IsNullOrEmpty(channelName))
            {
                throw new ArgumentNullException(nameof(channelName));
            }

            services.RegisterService(channelName, typeof(IMessageChannel));
            services.AddSingleton<IMessageChannel>((p) =>
            {
                var context = p.GetService<IApplicationContext>();
                var chan = new DirectChannel(context)
                {
                    ServiceName = channelName
                };

                configure?.Invoke(p, chan);

                return chan;
            });

            services.RegisterService(channelName, typeof(ISubscribableChannel));
            services.AddSingleton((p) =>
            {
                var context = p.GetService<IApplicationContext>();
                return GetRequiredChannel<ISubscribableChannel>(context, channelName);
            });

            return services;
        }

        public static IServiceCollection AddLoggingEndpoint(this IServiceCollection services)
        {
            services.AddSingleton<ILifecycle>((p) =>
            {
                var context = p.GetRequiredService<IApplicationContext>();
                var logger = p.GetRequiredService<ILogger<LoggingHandler>>();
                var handler = new LoggingHandler(context, LogLevel.Error, logger);
                var errorChan = GetRequiredChannel<ISubscribableChannel>(context, IntegrationContextUtils.ERROR_CHANNEL_BEAN_NAME);
                return new EventDrivenConsumerEndpoint(context, errorChan, handler);
            });
            return services;
        }

        public static IServiceCollection AddIntegrationServices(this IServiceCollection services)
        {
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            services.TryAddSingleton<ILifecycleProcessor, DefaultLifecycleProcessor>();
            services.TryAddSingleton<DefaultDatatypeChannelMessageConverter>();
            services.TryAddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();

            services.AddNullChannel();
            services.AddErrorChannel();
            services.AddLoggingEndpoint();

            services.AddSingleton<ServiceActivatorAttributeProcessor>();

            // SpringIntegrationProperties
            return services;
        }

        public static IServiceCollection AddServiceActivators(this IServiceCollection services, params Type[] targetClasses)
        {
            foreach (var targetClass in targetClasses)
            {
                services.AddServiceActivators(targetClass);
            }

            return services;
        }

        public static IServiceCollection AddServiceActivators(this IServiceCollection services, Type targetClass)
        {
            var targetMethods = AttributeUtils.FindMethodsWithAttribute(
                targetClass,
                typeof(ServiceActivatorAttribute),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var method in targetMethods)
            {
                services.AddServiceActivator(method, targetClass);
            }

            if (targetMethods.Count > 0)
            {
                services.TryAddSingleton(targetClass);
            }

            return services;
        }

        public static IServiceCollection AddServiceActivators<T>(this IServiceCollection services)
        {
            return services.AddServiceActivators(typeof(T));
        }

        public static IServiceCollection AddServiceActivator(this IServiceCollection services, MethodInfo method, Type targetClass)
        {
            var attribute = method.GetCustomAttribute<ServiceActivatorAttribute>();
            if (attribute == null)
            {
                throw new InvalidOperationException($"Method: '{method}' missing ServiceActivatorAttribute");
            }

            services.AddSingleton<IServiceActivatorMethod>(new ServiceActivatorMethod(method, targetClass, attribute));
            return services;
        }

        private static T GetRequiredChannel<T>(IApplicationContext context, string name)
            where T : class
        {
            if (context.GetServices<IMessageChannel>().FirstOrDefault((chan) => chan.ServiceName == name) is not T result)
            {
                throw new InvalidOperationException($"Unable to resolve channel:{name}");
            }

            return result;
        }
    }
}
