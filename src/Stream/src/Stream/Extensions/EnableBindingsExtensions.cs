// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;
using Steeltoe.Messaging;
using Steeltoe.Stream.Attributes;
using Steeltoe.Stream.Binding;
using Steeltoe.Stream.Messaging;

namespace Steeltoe.Stream.Extensions;

public static class EnableBindingsExtensions
{
    public static IServiceCollection AddEnableBinding<T>(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        Type type = typeof(T);
        object attr = type.GetCustomAttributes(true).SingleOrDefault(attr => attr.GetType() == typeof(EnableBindingAttribute));

        if (attr != null)
        {
            var enableBindingAttribute = (EnableBindingAttribute)attr;

            Type[] filtered = enableBindingAttribute.BindingTypes
                .Where(b => b.Name != nameof(ISource) && b.Name != nameof(ISink) && b.Name != nameof(IProcessor)).ToArray(); // These are added by default

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
        ArgumentGuard.NotNull(services);

        return services.AddStreamBinding<IProcessor>();
    }

    public static IServiceCollection AddSinkStreamBinding(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        return services.AddStreamBinding<ISink>();
    }

    public static IServiceCollection AddSourceStreamBinding(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        return services.AddStreamBinding<ISource>();
    }

    public static IServiceCollection AddDefaultBindings(this IServiceCollection services)
    {
        services.AddProcessorStreamBinding();
        return services;
    }

    public static IServiceCollection AddStreamBinding<TBinding>(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        return services.AddStreamBindings(typeof(TBinding));
    }

    public static IServiceCollection AddStreamBindings<TBinding1, TBinding2>(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        return services.AddStreamBindings(typeof(TBinding1), typeof(TBinding2));
    }

    public static IServiceCollection AddStreamBindings<TBinding1, TBinding2, TBinding3>(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        return services.AddStreamBindings(typeof(TBinding1), typeof(TBinding2), typeof(TBinding3));
    }

    public static IServiceCollection AddStreamBindings(this IServiceCollection services, params Type[] bindingTypes)
    {
        ArgumentGuard.NotNullOrEmpty(services);

        // Add all the bindings to container
        services.AddBindings(bindingTypes);

        return services;
    }

    internal static void AddBindings(this IServiceCollection services, Type[] bindingTypes)
    {
        // TODO: Verify all binding types unique
        foreach (Type bindingType in bindingTypes)
        {
            // Validate binding interface
            if (!bindingType.IsInterface || !bindingType.IsPublic || bindingType.IsGenericType)
            {
                throw new ArgumentException($"Binding {bindingType} is incorrectly defined.", nameof(bindingTypes));
            }

            // Add the binding to container
            services.AddBinding(bindingType);
        }
    }

    internal static void AddBinding(this IServiceCollection services, Type bindingType)
    {
        AddBindableTargets(services, bindingType);

        // Add the IBindable for this binding (i.e. BindableProxyFactory)
        services.AddSingleton<IBindable>(p =>
        {
            IEnumerable<IBindingTargetFactory> bindingTargetFactories = p.GetServices<IBindingTargetFactory>();
            return new BindableProxyFactory(bindingType, bindingTargetFactories);
        });

        // Add Binding (ISink, IProcessor, IFooBar)
        services.AddSingleton(bindingType, p =>
        {
            // Find the bindabe for this binding
            IEnumerable<IBindable> bindables = p.GetServices<IBindable>();
            IBindable bindable = bindables.SingleOrDefault(b => b.BindingType == bindingType);

            if (bindable == null)
            {
                throw new InvalidOperationException("Unable to find bindable for binding");
            }

            return BindingProxyGenerator.CreateProxy((BindableProxyFactory)bindable);
        });

        List<Type> derivedInterfaces = bindingType.FindInterfaces((_, _) => true, null).ToList();

        foreach (Type derived in derivedInterfaces)
        {
            services.AddSingleton(derived, p => p.GetService(bindingType));
        }
    }

    internal static void AddBindableTargets(this IServiceCollection services, Type bindingType)
    {
        IDictionary<string, Bindable> bindables = BindingHelpers.CollectBindables(bindingType);

        foreach (Bindable bindable in bindables.Values)
        {
            // Add bindable defined in a binding
            Type bindableTargetType = bindable.BindingTargetType;

            services.AddSingleton(bindableTargetType, p =>
            {
                object impl = p.GetRequiredService(bindingType);
                object result = bindable.FactoryMethod.Invoke(impl, Array.Empty<object>());
                return result;
            });

            // Also register an IMessageChannel if bindableTargetType is a IMessageChannel
            if (bindableTargetType != typeof(IMessageChannel) && typeof(IMessageChannel).IsAssignableFrom(bindableTargetType))
            {
                services.AddSingleton(typeof(IMessageChannel), p =>
                {
                    object impl = p.GetRequiredService(bindingType);
                    object result = bindable.FactoryMethod.Invoke(impl, Array.Empty<object>());
                    return result;
                });
            }
        }
    }
}
