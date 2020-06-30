// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Messaging.Rabbit.Config;
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Host;
using Steeltoe.Messaging.Rabbit.Listener;
using System;
using System.Linq;

namespace Steeltoe.Messaging.Rabbit.Extensions
{
    public static class RabbitServicesExtensions
    {
        public static IServiceCollection AddRabbitTemplate(this IServiceCollection services)
        {
            return services.AddRabbitTemplate(RabbitTemplate.DEFAULT_SERVICE_NAME, null);
        }

        public static IServiceCollection AddRabbitTemplate(this IServiceCollection services, Action<IServiceProvider, RabbitTemplate> configure)
        {
            return services.AddRabbitTemplate(RabbitTemplate.DEFAULT_SERVICE_NAME, configure);
        }

        public static IServiceCollection AddRabbitTemplate(this IServiceCollection services, string serviceName, Action<IServiceProvider, RabbitTemplate> configure = null)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException(nameof(serviceName));
            }

            services.AddSingleton((p) =>
            {
                var template = ActivatorUtilities.CreateInstance(p, typeof(RabbitTemplate)) as RabbitTemplate;
                template.ServiceName = serviceName;
                if (configure != null)
                {
                    configure(p, template);
                }

                return template;
            });
            services.AddSingleton<IRabbitTemplate>((p) =>
            {
                return p.GetServices<RabbitTemplate>().SingleOrDefault((t) => t.ServiceName == serviceName);
            });

            return services;
        }

        public static IServiceCollection AddRabbitAdmin(this IServiceCollection services)
        {
            return services.AddRabbitAdmin(RabbitAdmin.DEFAULT_SERVICE_NAME, null);
        }

        public static IServiceCollection AddRabbitAdmin(this IServiceCollection services, Action<IServiceProvider, RabbitAdmin> configure)
        {
            return services.AddRabbitAdmin(RabbitAdmin.DEFAULT_SERVICE_NAME, configure);
        }

        public static IServiceCollection AddRabbitAdmin(this IServiceCollection services, string serviceName, Action<IServiceProvider, RabbitAdmin> configure = null)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException(nameof(serviceName));
            }

            services.AddSingleton((p) =>
            {
                var admin = ActivatorUtilities.CreateInstance(p, typeof(RabbitAdmin)) as RabbitAdmin;
                admin.ServiceName = serviceName;
                if (configure != null)
                {
                    configure(p, admin);
                }

                return admin;
            });
            services.AddSingleton<IRabbitAdmin>((p) =>
            {
                return p.GetServices<RabbitAdmin>().SingleOrDefault((t) => t.ServiceName == serviceName);
            });
            return services;
        }

        public static IServiceCollection AddRabbitQueues(this IServiceCollection services, params IQueue[] queues)
        {
            foreach (var q in queues)
            {
                services.AddRabbitQueue(q);
            }

            return services;
        }

        public static IServiceCollection AddRabbitQueue(this IServiceCollection services, IQueue queue)
        {
            return services.AddSingleton(queue);
        }

        public static IServiceCollection AddRabbitQueue(this IServiceCollection services, string queueName, Action<IServiceProvider, Queue> configure = null)
        {
            services.AddSingleton<IQueue>(p =>
            {
                var queue = new Queue(queueName);
                if (configure != null)
                {
                    configure(p, queue);
                }

                return queue;
            });

            return services;
        }

        public static IServiceCollection AddRabbitExchanges(this IServiceCollection services, params IExchange[] exchanges)
        {
            foreach (var e in exchanges)
            {
                services.AddRabbitExchange(e);
            }

            return services;
        }

        public static IServiceCollection AddRabbitExchange(this IServiceCollection services, IExchange exchange)
        {
            return services.AddSingleton<IExchange>(exchange);
        }

        public static IServiceCollection AddRabbitExchange(this IServiceCollection services, string exchangeName, string exchangeType, Action<IServiceProvider, IExchange> configure = null)
        {
            if (string.IsNullOrEmpty(exchangeName))
            {
                throw new ArgumentException(nameof(exchangeName));
            }

            if (string.IsNullOrEmpty(exchangeType))
            {
                throw new ArgumentException(nameof(exchangeType));
            }

            services.AddSingleton<IExchange>(p =>
            {
                var exchange = ExchangeBuilder.Create(exchangeName, exchangeType);
                if (configure != null)
                {
                    configure(p, exchange);
                }

                return exchange;
            });

            return services;
        }

        public static IServiceCollection AddRabbitBindings(this IServiceCollection services, params IBinding[] bindings)
        {
            foreach (var b in bindings)
            {
                services.AddRabbitBinding(b);
            }

            return services;
        }

        public static IServiceCollection AddRabbitBinding(this IServiceCollection services, IBinding binding)
        {
            services.AddSingleton<IBinding>(binding);
            return services;
        }

        public static IServiceCollection AddRabbitBinding(this IServiceCollection services, string bindingName, Binding.DestinationType bindingType, Action<IServiceProvider, IBinding> configure = null)
        {
            if (string.IsNullOrEmpty(bindingName))
            {
                throw new ArgumentException(nameof(bindingName));
            }

            services.AddSingleton<IBinding>(p =>
            {
                var binding = BindingBuilder.Create(bindingName, bindingType);
                if (configure != null)
                {
                    configure(p, binding);
                }

                return binding;
            });
            return services;
        }

        public static IServiceCollection ConfigureRabbitOptions(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<RabbitOptions>(config.GetSection(RabbitOptions.PREFIX));
            return services;
        }

        public static IServiceCollection AddRabbitServices(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddRabbitHostingServices();

            services.AddRabbitConnectionFactory();
            services.AddRabbitDefaultMessageConverter();
            services.AddRabbitMessageHandlerMethodFactory();
            services.AddRabbitListenerContainerFactory();
            services.AddRabbitListenerEndpointRegistry();
            services.AddRabbitListenerEndpointRegistrar();
            services.AddRabbitListenerAttributeProcessor();
            return services;
        }

        public static IServiceCollection AddRabbitHostingServices(this IServiceCollection services)
        {
            services.AddOptions();
            services.AddHostedService<RabbitHostService>();
            services.TryAddSingleton<ILifecycleProcessor, DefaultLifecycleProcessor>();
            services.TryAddSingleton<IApplicationContext, GenericApplicationContext>();

            return services;
        }

        public static IServiceCollection AddRabbitListenerAttributeProcessor(this IServiceCollection services, Action<IServiceProvider, RabbitListenerAttributeProcessor> configure = null)
        {
            return services.AddRabbitListenerAttributeProcessor<RabbitListenerAttributeProcessor>(RabbitListenerAttributeProcessor.DEFAULT_SERVICE_NAME, configure);
        }

        public static IServiceCollection AddRabbitListenerAttributeProcessor(this IServiceCollection services, string serviceName, Action<IServiceProvider, RabbitListenerAttributeProcessor> configure = null)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException(nameof(serviceName));
            }

            services.AddRabbitListenerAttributeProcessor<RabbitListenerAttributeProcessor>(serviceName, configure);
            return services;
        }

        public static IServiceCollection AddRabbitListenerAttributeProcessor<P>(this IServiceCollection services, Action<IServiceProvider, P> configure)
            where P : IRabbitListenerAttributeProcessor
        {
            return services.AddRabbitListenerAttributeProcessor<P>(null, configure);
        }

        public static IServiceCollection AddRabbitListenerAttributeProcessor<P>(this IServiceCollection services, string serviceName = null, Action<IServiceProvider, P> configure = null)
            where P : IRabbitListenerAttributeProcessor
        {
            services.TryAddSingleton<IRabbitListenerAttributeProcessor>((p) =>
            {
                var instance = (P)ActivatorUtilities.GetServiceOrCreateInstance(p, typeof(P));
                if (!string.IsNullOrEmpty(serviceName))
                {
                    instance.ServiceName = serviceName;
                }

                if (configure != null)
                {
                    configure(p, instance);
                }

                return instance;
            });

            return services;
        }

        public static IServiceCollection AddRabbitListenerEndpointRegistrar(this IServiceCollection services, Action<IServiceProvider, RabbitListenerEndpointRegistrar> configure = null)
        {
            services.AddRabbitListenerEndpointRegistrar<RabbitListenerEndpointRegistrar>(RabbitListenerEndpointRegistrar.DEFAULT_SERVICE_NAME, configure);
            return services;
        }

        public static IServiceCollection AddRabbitListenerEndpointRegistrar(this IServiceCollection services, string serviceName, Action<IServiceProvider, RabbitListenerEndpointRegistrar> configure = null)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException(nameof(serviceName));
            }

            services.AddRabbitListenerEndpointRegistrar<RabbitListenerEndpointRegistrar>(serviceName, configure);
            return services;
        }

        public static IServiceCollection AddRabbitListenerEndpointRegistrar<R>(this IServiceCollection services, Action<IServiceProvider, R> configure)
            where R : IRabbitListenerEndpointRegistrar
        {
            return services.AddRabbitListenerEndpointRegistrar<R>(null, configure);
        }

        public static IServiceCollection AddRabbitListenerEndpointRegistrar<R>(this IServiceCollection services, string serviceName = null, Action<IServiceProvider, R> configure = null)
            where R : IRabbitListenerEndpointRegistrar
        {
            services.TryAddSingleton<IRabbitListenerEndpointRegistrar>((p) =>
            {
                var instance = (R)ActivatorUtilities.GetServiceOrCreateInstance(p, typeof(R));
                if (!string.IsNullOrEmpty(serviceName))
                {
                    instance.ServiceName = serviceName;
                }

                if (configure != null)
                {
                    configure(p, instance);
                }

                return instance;
            });

            return services;
        }

        public static IServiceCollection AddRabbitListenerEndpointRegistry(this IServiceCollection services, Action<IServiceProvider, RabbitListenerEndpointRegistry> configure = null)
        {
            services.AddRabbitListenerEndpointRegistry<RabbitListenerEndpointRegistry>(RabbitListenerEndpointRegistry.DEFAULT_SERVICE_NAME, configure);
            return services;
        }

        public static IServiceCollection AddRabbitListenerEndpointRegistry(this IServiceCollection services, string serviceName, Action<IServiceProvider, RabbitListenerEndpointRegistry> configure = null)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException(nameof(serviceName));
            }

            services.AddRabbitListenerEndpointRegistry<RabbitListenerEndpointRegistry>(serviceName, configure);
            return services;
        }

        public static IServiceCollection AddRabbitListenerEndpointRegistry<R>(this IServiceCollection services, Action<IServiceProvider, R> configure)
            where R : IRabbitListenerEndpointRegistry
        {
            return services.AddRabbitListenerEndpointRegistry<R>(null, configure);
        }

        public static IServiceCollection AddRabbitListenerEndpointRegistry<R>(this IServiceCollection services, string serviceName = null, Action<IServiceProvider, R> configure = null)
            where R : IRabbitListenerEndpointRegistry
        {
            services.TryAddSingleton<IRabbitListenerEndpointRegistry>((p) =>
            {
                var instance = (R)ActivatorUtilities.GetServiceOrCreateInstance(p, typeof(R));
                if (!string.IsNullOrEmpty(serviceName))
                {
                    instance.ServiceName = serviceName;
                }

                if (configure != null)
                {
                    configure(p, instance);
                }

                return instance;
            });

            services.AddSingleton<ILifecycle>((p) =>
            {
                var instance = p.GetRequiredService<IRabbitListenerEndpointRegistry>() as ILifecycle;
                return instance;
            });

            return services;
        }

        public static IServiceCollection AddRabbitListenerContainerFactory(this IServiceCollection services, Action<IServiceProvider, DirectRabbitListenerContainerFactory> configure = null)
        {
            return services.AddRabbitListenerContainerFactory<DirectRabbitListenerContainerFactory>(DirectRabbitListenerContainerFactory.DEFAULT_SERVICE_NAME, configure);
        }

        public static IServiceCollection AddRabbitListenerContainerFactory(this IServiceCollection services, string serviceName, Action<IServiceProvider, DirectRabbitListenerContainerFactory> configure = null)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentNullException(serviceName);
            }

            return services.AddRabbitListenerContainerFactory<DirectRabbitListenerContainerFactory>(serviceName, configure);
        }

        public static IServiceCollection AddRabbitListenerContainerFactory<F>(this IServiceCollection services, Action<IServiceProvider, F> configure)
            where F : IRabbitListenerContainerFactory
        {
            return services.AddRabbitListenerContainerFactory<F>(null, configure);
        }

        public static IServiceCollection AddRabbitListenerContainerFactory<F>(this IServiceCollection services, string serviceName = null, Action<IServiceProvider, F> configure = null)
            where F : IRabbitListenerContainerFactory
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException(nameof(serviceName));
            }

            services.AddSingleton<IRabbitListenerContainerFactory>((p) =>
            {
                var instance = (F)ActivatorUtilities.CreateInstance(p, typeof(F));
                if (!string.IsNullOrEmpty(serviceName))
                {
                    instance.ServiceName = serviceName;
                }

                if (configure != null)
                {
                    configure(p, instance);
                }

                return instance;
            });
            return services;
        }

        public static IServiceCollection AddRabbitConnectionFactory(this IServiceCollection services, Action<IServiceProvider, CachingConnectionFactory> configure = null)
        {
            return services.AddRabbitConnectionFactory<CachingConnectionFactory>(CachingConnectionFactory.DEFAULT_SERVICE_NAME, configure);
        }

        public static IServiceCollection AddRabbitConnectionFactory(this IServiceCollection services, string serviceName, Action<IServiceProvider, CachingConnectionFactory> configure = null)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException(nameof(serviceName));
            }

            return services.AddRabbitConnectionFactory<CachingConnectionFactory>(serviceName, configure);
        }

        public static IServiceCollection AddRabbitConnectionFactory<F>(this IServiceCollection services, Action<IServiceProvider, F> configure)
            where F : IConnectionFactory
        {
            return services.AddRabbitConnectionFactory<F>(null, configure);
        }

        public static IServiceCollection AddRabbitConnectionFactory<F>(this IServiceCollection services, string serviceName = null, Action<IServiceProvider, F> configure = null)
            where F : IConnectionFactory
        {
            services.AddSingleton<IConnectionFactory>(p =>
           {
               var instance = (F)ActivatorUtilities.GetServiceOrCreateInstance(p, typeof(F));
               if (!string.IsNullOrEmpty(serviceName))
               {
                   instance.ServiceName = serviceName;
               }

               if (configure != null)
               {
                   configure(p, instance);
               }

               return instance;
           });

            return services;
        }

        public static IServiceCollection AddRabbitJsonMessageConverter(this IServiceCollection services, Action<IServiceProvider, Support.Converter.JsonMessageConverter> configure = null)
        {
            return services.AddRabbitMessageConverter<Support.Converter.JsonMessageConverter>(Support.Converter.JsonMessageConverter.DEFAULT_SERVICE_NAME, configure);
        }

        public static IServiceCollection AddRabbitJsonMessageConverter(this IServiceCollection services, string serviceName, Action<IServiceProvider, Support.Converter.JsonMessageConverter> configure = null)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException(serviceName);
            }

            return services.AddRabbitMessageConverter<Support.Converter.JsonMessageConverter>(serviceName, configure);
        }

        public static IServiceCollection AddRabbitDefaultMessageConverter(this IServiceCollection services, Action<IServiceProvider, Support.Converter.SimpleMessageConverter> configure = null)
        {
            return services.AddRabbitMessageConverter<Support.Converter.SimpleMessageConverter>(SimpleMessageConverter.DEFAULT_SERVICE_NAME, configure);
        }

        public static IServiceCollection AddRabbitDefaultMessageConverter(this IServiceCollection services, string serviceName, Action<IServiceProvider, Support.Converter.SimpleMessageConverter> configure = null)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException(serviceName);
            }

            return services.AddRabbitMessageConverter<Support.Converter.SimpleMessageConverter>(serviceName, configure);
        }

        public static IServiceCollection AddRabbitMessageConverter<C>(this IServiceCollection services, Action<IServiceProvider, C> configure)
            where C : ISmartMessageConverter
        {
            return services.AddRabbitMessageConverter<C>(null, configure);
        }

        public static IServiceCollection AddRabbitMessageConverter<C>(this IServiceCollection services, string serviceName = null, Action<IServiceProvider, C> configure = null)
           where C : ISmartMessageConverter
        {
            services.TryAddSingleton<ISmartMessageConverter>((p) =>
            {
                var instance = (C)ActivatorUtilities.GetServiceOrCreateInstance(p, typeof(C));
                if (!string.IsNullOrEmpty(serviceName))
                {
                    instance.ServiceName = serviceName;
                }

                if (configure != null)
                {
                    configure(p, instance);
                }

                return instance;
            });

            return services;
        }

        public static IServiceCollection AddRabbitMessageHandlerMethodFactory(this IServiceCollection services, Action<IServiceProvider, RabbitMessageHandlerMethodFactory> configure = null)
        {
            return services.AddRabbitMessageHandlerMethodFactory<RabbitMessageHandlerMethodFactory>(RabbitMessageHandlerMethodFactory.DEFAULT_SERVICE_NAME, configure);
        }

        public static IServiceCollection AddRabbitMessageHandlerMethodFactory(this IServiceCollection services, string serviceName, Action<IServiceProvider, RabbitMessageHandlerMethodFactory> configure = null)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException(serviceName);
            }

            return services.AddRabbitMessageHandlerMethodFactory<RabbitMessageHandlerMethodFactory>(serviceName, configure);
        }

        public static IServiceCollection AddRabbitMessageHandlerMethodFactory<F>(this IServiceCollection services, Action<IServiceProvider, F> configure)
            where F : IMessageHandlerMethodFactory
        {
            return services.AddRabbitMessageHandlerMethodFactory<F>(null, configure);
        }

        public static IServiceCollection AddRabbitMessageHandlerMethodFactory<F>(this IServiceCollection services, string serviceName = null, Action<IServiceProvider, F> configure = null)
           where F : IMessageHandlerMethodFactory
        {
            services.TryAddSingleton<IMessageHandlerMethodFactory>((p) =>
            {
                var instance = (F)ActivatorUtilities.GetServiceOrCreateInstance(p, typeof(F));
                if (!string.IsNullOrEmpty(serviceName))
                {
                    instance.ServiceName = serviceName;
                }

                if (configure != null)
                {
                    configure(p, instance);
                }

                instance.Initialize();

                return instance;
            });

            return services;
        }
    }
}
