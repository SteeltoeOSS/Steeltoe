// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static Steeltoe.Common.Contexts.AbstractApplicationContext;

namespace Steeltoe.Common.Contexts;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGenericApplicationContext(this IServiceCollection services,
        Action<IServiceProvider, GenericApplicationContext> configure)
    {
        services.TryAddSingleton<IApplicationContext>(p =>
        {
            var configuration = p.GetService<IConfiguration>();
            var context = new GenericApplicationContext(p, configuration);

            if (configure != null)
            {
                configure(p, context);
            }

            return context;
        });

        return services;
    }

    public static IServiceCollection AddGenericApplicationContext(this IServiceCollection services)
    {
        return services.AddGenericApplicationContext(null);
    }

    public static IServiceCollection RegisterService(this IServiceCollection services, string serviceName, Type implementationType)
    {
        services.AddSingleton(new NameToTypeMapping(serviceName, implementationType));
        return services;
    }
}
