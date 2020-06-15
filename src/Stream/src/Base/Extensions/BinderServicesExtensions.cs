// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Stream.Binder;
using Steeltoe.Stream.Config;
using System;
using System.Reflection;

namespace Steeltoe.Stream.Extensions
{
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
            foreach (var binderConfiguration in binderConfigurations.Configurations)
            {
                var type = FindConfigureType(binderConfiguration.Value);
                if (type != null)
                {
                    var constr = FindConstructor(type);
                    var method = FindConfigureServicesMethod(type);
                    if (constr != null && method != null)
                    {
                        try
                        {
                            var instance = constr.Invoke(new object[] { configuration });
                            if (instance != null)
                            {
                                method.Invoke(instance, new object[] { services });
                            }

                            binderConfiguration.Value.ResolvedAssembly = type.Assembly.Location;
                        }
                        catch (Exception)
                        {
                            // Log
                        }
                    }
                    else
                    {
                        // Log
                    }
                }
                else
                {
                    // Log
                }
            }
        }

        internal static MethodInfo FindConfigureServicesMethod(Type type)
        {
            return type.GetMethod("ConfigureServices", new Type[] { typeof(IServiceCollection) });
        }

        internal static Type FindConfigureType(BinderConfiguration binderConfiguration)
        {
            if (string.IsNullOrEmpty(binderConfiguration.ConfigureAssembly))
            {
                return Type.GetType(binderConfiguration.ConfigureClass, false);
            }
            else
            {
#pragma warning disable S3885 // "Assembly.Load" should be used
                var assembly = Assembly.LoadFrom(binderConfiguration.ConfigureAssembly);
#pragma warning restore S3885 // "Assembly.Load" should be used
                return assembly.GetType(binderConfiguration.ConfigureClass.Split(',')[0], false);
            }
        }

        internal static ConstructorInfo FindConstructor(Type type)
        {
            var constr = type.GetConstructor(new Type[] { typeof(IConfiguration) });
            if (constr == null)
            {
                constr = type.GetConstructor(new Type[] { });
            }

            return constr;
        }
    }
}
