// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Stream.Binder;
using Steeltoe.Stream.Config;

namespace Steeltoe.Stream.Extensions;

public static class BinderServicesExtensions
{
    public static IServiceCollection AddBinderServices(this IServiceCollection services, IConfiguration configuration)
    {
        var registry = new DefaultBinderTypeRegistry();
        services.TryAddSingleton<IBinderTypeRegistry>(registry);
        services.AddBinderServices(registry, configuration);

        return services;
    }

    internal static void AddBinderServices(this IServiceCollection services, IBinderTypeRegistry registry, IConfiguration configuration)
    {
        var binderConfigurations = new BinderConfigurations(registry, configuration.GetSection("spring:cloud:stream"));
        services.TryAddSingleton<IBinderConfigurations>(binderConfigurations);
        services.AddBinderServices(binderConfigurations, configuration);
    }

    internal static void AddBinderServices(this IServiceCollection services, IBinderConfigurations binderConfigurations, IConfiguration configuration)
    {
        services.TryAddSingleton<IBinderFactory, DefaultBinderFactory>();
        services.ConfigureBinderServices(binderConfigurations, configuration);
    }

    internal static void ConfigureBinderServices(this IServiceCollection services, IBinderConfigurations binderConfigurations, IConfiguration configuration)
    {
        foreach (KeyValuePair<string, BinderConfiguration> binderConfiguration in binderConfigurations.Configurations)
        {
            Type type = FindConfigureType(binderConfiguration.Value);

            if (type != null)
            {
                ConstructorInfo constructor = FindConstructor(type);
                MethodInfo method = FindConfigureServicesMethod(type);

                if (constructor != null && method != null)
                {
                    try
                    {
                        object instance = constructor.Invoke(new object[]
                        {
                            configuration
                        });

                        if (instance != null)
                        {
                            method.Invoke(instance, new object[]
                            {
                                services
                            });
                        }

                        binderConfiguration.Value.ResolvedAssembly = type.Assembly.Location;
                    }
                    catch (Exception)
                    {
                        // Log
                    }
                }
            }
        }
    }

    internal static MethodInfo FindConfigureServicesMethod(Type type)
    {
        return type.GetMethod("ConfigureServices", new[]
        {
            typeof(IServiceCollection)
        });
    }

    internal static Type FindConfigureType(BinderConfiguration binderConfiguration)
    {
        if (string.IsNullOrEmpty(binderConfiguration.ConfigureAssembly))
        {
            return Type.GetType(binderConfiguration.ConfigureClass, false);
        }
#pragma warning disable S3885 // "Assembly.Load" should be used
        Assembly assembly = Assembly.LoadFrom(binderConfiguration.ConfigureAssembly);
#pragma warning restore S3885 // "Assembly.Load" should be used
        return assembly.GetType(binderConfiguration.ConfigureClass.Split(',')[0], false);
    }

    internal static ConstructorInfo FindConstructor(Type type)
    {
        ConstructorInfo constructor = type.GetConstructor(new[]
        {
            typeof(IConfiguration)
        });

        if (constructor == null)
        {
            constructor = type.GetConstructor(Array.Empty<Type>());
        }

        return constructor;
    }
}
