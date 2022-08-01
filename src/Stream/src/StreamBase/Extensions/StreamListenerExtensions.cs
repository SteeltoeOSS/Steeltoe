// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Stream.Attributes;
using Steeltoe.Stream.Binding;
using Steeltoe.Stream.Config;
using System.Reflection;

namespace Steeltoe.Stream.Extensions;

public static class StreamListenerExtensions
{
    public static IServiceCollection AddStreamListeners<T>(this IServiceCollection services)
        where T : class
        => services.AddStreamListeners(typeof(T));

    public static IServiceCollection AddStreamListeners(this IServiceCollection services, Type type)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        var targetMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

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
            services.TryAddSingleton(type);
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
