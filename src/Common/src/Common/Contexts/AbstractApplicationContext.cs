// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Common.Contexts
{
    public abstract class AbstractApplicationContext : IApplicationContext
    {
        public AbstractApplicationContext(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            ServiceProvider = serviceProvider;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public IServiceProvider ServiceProvider { get; }

        public bool ContainsService(string name, Type serviceType)
        {
            if (!typeof(IServiceNameAware).IsAssignableFrom(serviceType))
            {
                return false;
            }

            var found = ServiceProvider.GetServices(serviceType).SingleOrDefault<object>((service) =>
            {
                if (service is IServiceNameAware nameAware)
                {
                    return nameAware.ServiceName == name;
                }

                return false;
            });

            return found != null;
        }

        public bool ContainsService<T>(string name)
        {
            return ContainsService(name, typeof(T));
        }

        public object GetService(string name, Type serviceType)
        {
            if (!typeof(IServiceNameAware).IsAssignableFrom(serviceType))
            {
                return null;
            }

            var found = ServiceProvider.GetServices(serviceType).SingleOrDefault<object>((service) =>
            {
                if (service is IServiceNameAware nameAware)
                {
                    return nameAware.ServiceName == name;
                }

                return false;
            });

            return found;
        }

        public T GetService<T>(string name)
        {
            return (T)GetService(name, typeof(T));
        }

        public T GetService<T>()
        {
            return ServiceProvider.GetService<T>();
        }

        public object GetService(Type serviceType)
        {
            return ServiceProvider.GetService(serviceType);
        }

        public IEnumerable<T> GetServices<T>()
        {
            return ServiceProvider.GetServices<T>();
        }
    }
}
