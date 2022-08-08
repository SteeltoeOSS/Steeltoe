// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;
using Steeltoe.Stream.Attributes;
using Steeltoe.Stream.Binding;
using Steeltoe.Stream.Config;

namespace Steeltoe.Stream.Extensions;

public static class StreamListenerExtensions
{
    public static IServiceCollection AddStreamListeners<T>(this IServiceCollection services)
        where T : class
    {
        return services.AddStreamListeners(typeof(T));
    }

    public static IServiceCollection AddStreamListeners(this IServiceCollection services, Type type)
    {
        ArgumentGuard.NotNull(services);

        MethodInfo[] targetMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        bool listenersAdded = false;

        foreach (MethodInfo method in targetMethods)
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

    public static IServiceCollection AddStreamListener(this IServiceCollection services, MethodInfo method, string target, string condition = null,
        bool copyHeaders = true)
    {
        ArgumentGuard.NotNull(services);

        var attribute = new StreamListenerAttribute(target, condition, copyHeaders);
        services.AddStreamListener(method, attribute);
        return services;
    }
}
