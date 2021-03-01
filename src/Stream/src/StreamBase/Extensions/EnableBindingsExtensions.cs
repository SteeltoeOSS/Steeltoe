// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common.Reflection;
using Steeltoe.Messaging;
using Steeltoe.Stream.Attributes;
using Steeltoe.Stream.Binding;
using Steeltoe.Stream.Messaging;
using System;
using System.Linq;
using System.Reflection;

namespace Steeltoe.Stream.Extensions
{
    public static class EnableBindingsExtensions
    {
        public static IServiceCollection AddEnableBinding<T>(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var type = typeof(T);
            var attr = type.GetCustomAttributes(true).SingleOrDefault(attr => attr.GetType() == typeof(EnableBindingAttribute));

            if (attr != null)
            {
                var enableBindingAttribute = (EnableBindingAttribute)attr;
                var filtered = enableBindingAttribute.Bindings.Where(b => b.Name != nameof(ISource) && b.Name != nameof(ISink) && b.Name != nameof(IProcessor)).ToArray(); // These are added by default
                if (filtered.Length > 0)
                {
                    services.AddStreamBindings(filtered);
                }
                services.AddStreamListeners(type);
                services.TryAddSingleton(type);
            }

            return services;
        }

        public static IServiceCollection AddProcessorStreamBinding(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddStreamBinding<IProcessor>();
        }

        public static IServiceCollection AddSinkStreamBinding(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddStreamBinding<ISink>();
        }

        public static IServiceCollection AddSourceStreamBinding(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddStreamBinding<ISource>();
        }

        public static IServiceCollection AddDefaultBindings(this IServiceCollection services)
        {
            services.AddProcessorStreamBinding();
            return services;
        }

        public static IServiceCollection AddStreamBinding<B>(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddStreamBindings(typeof(B));
        }

        public static IServiceCollection AddStreamBindings<B1, B2>(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddStreamBindings(typeof(B1), typeof(B2));
        }

        public static IServiceCollection AddStreamBindings<B1, B2, B3>(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddStreamBindings(typeof(B1), typeof(B2), typeof(B3));
        }

        public static IServiceCollection AddStreamBindings(this IServiceCollection services, params Type[] bindings)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (bindings == null || bindings.Length == 0)
            {
                throw new ArgumentException("Must provide one or more bindings");
            }

            // Add all the bindings to container
            services.AddBindings(bindings);

            return services;
        }

        internal static void AddBindings(this IServiceCollection services, Type[] bindings)
        {
            // TODO: Verify all binding types unique
            foreach (var binding in bindings)
            {
                // Validate binding interface
                if (!binding.IsInterface || !binding.IsPublic || binding.IsGenericType)
                {
                    throw new ArgumentException($"Binding {binding} incorrectly defined");
                }

                // Add the binding to container
                services.AddBinding(binding);
            }
        }

        internal static void AddBinding(this IServiceCollection services, Type binding)
        {
            AddBindableTargets(services, binding);

            // Add the IBindable for this binding (i.e. BindableProxyFactory)
            services.AddSingleton<IBindable>((p) =>
            {
                var bindingTargetFactories = p.GetServices<IBindingTargetFactory>();
                return new BindableProxyFactory(binding, bindingTargetFactories);
            });

            // Add Binding (ISink, IProcessor, IFooBar)
            services.AddSingleton(binding, (p) =>
            {
                // Find the bindabe for this binding
                var bindables = p.GetServices<IBindable>();
                var bindable = bindables.SingleOrDefault((b) => b.BindingType == binding);
                if (bindable == null)
                {
                    throw new InvalidOperationException("Unable to find bindable for binding");
                }

                return BindingProxyGenerator.CreateProxy((BindableProxyFactory)bindable);
            });

            var derivedInterfaces = binding.FindInterfaces((t, c) => true, null).ToList();
            foreach (var derived in derivedInterfaces)
            {
                services.AddSingleton(derived, (p) => p.GetService(binding));
            }
        }

        internal static void AddBindableTargets(this IServiceCollection services, Type binding)
        {
            var bindables = BindingHelpers.CollectBindables(binding);
            foreach (var bindable in bindables.Values)
            {
                // Add bindable defined in a binding
                var bindableTargetType = bindable.BindingTargetType;
                services.AddSingleton(bindableTargetType, (p) =>
                {
                    var impl = p.GetRequiredService(binding);
                    var result = bindable.FactoryMethod.Invoke(impl, new object[0]);
                    return result;
                });

                // Also register an IMessageChannel if bindableTargetType is a IMessageChannel
                if (bindableTargetType != typeof(IMessageChannel) && typeof(IMessageChannel).IsAssignableFrom(bindableTargetType))
                {
                    services.AddSingleton(typeof(IMessageChannel), (p) =>
                    {
                        var impl = p.GetRequiredService(binding);
                        var result = bindable.FactoryMethod.Invoke(impl, new object[0]);
                        return result;
                    });
                }
            }
        }
    }
}
