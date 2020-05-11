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
using Steeltoe.Messaging;
using Steeltoe.Stream.Binding;
using Steeltoe.Stream.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Stream.Extensions
{
    public static class EnableBindingsExtenstions
    {
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
                    var result = bindable.FactoryMethod.Invoke(impl, Array.Empty<object>());
                    return result;
                });

                // Also register an IMessageChannel if bindableTargetType is a IMessageChannel
                if (bindableTargetType != typeof(IMessageChannel) && typeof(IMessageChannel).IsAssignableFrom(bindableTargetType))
                {
                    services.AddSingleton(typeof(IMessageChannel), (p) =>
                    {
                        var impl = p.GetRequiredService(binding);
                        var result = bindable.FactoryMethod.Invoke(impl, Array.Empty<object>());
                        return result;
                    });
                }
            }
        }
    }
}
