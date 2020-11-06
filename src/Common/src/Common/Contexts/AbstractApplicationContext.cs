// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Common.Contexts
{
    public abstract class AbstractApplicationContext : IApplicationContext
    {
        private readonly ConcurrentDictionary<string, object> _instances = new ConcurrentDictionary<string, object>();

        public AbstractApplicationContext(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            ServiceProvider = serviceProvider;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public IServiceProvider ServiceProvider { get; }

        public bool ContainsService(string name, Type serviceType)
        {
            if (_instances.TryGetValue(name, out object instance))
            {
                return serviceType.IsInstanceOfType(instance);
            }

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

        public bool ContainsService(string name)
        {
            if (_instances.TryGetValue(name, out object instance))
            {
                return true;
            }

            // TODO: This will not work, do we need a work around?
            var found = ServiceProvider.GetServices(typeof(object)).SingleOrDefault<object>((service) =>
            {
                if (service is IServiceNameAware nameAware)
                {
                    return nameAware.ServiceName == name;
                }

                return false;
            });

            return found != null;
        }

        public object GetService(string name, Type serviceType)
        {
            if (_instances.TryGetValue(name, out var instance) && serviceType.IsInstanceOfType(instance))
            {
                return instance;
            }

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
            return (T)this.GetService(typeof(T));
        }

        public object GetService(Type serviceType)
        {
            var result = _instances.Values.LastOrDefault(instance => serviceType.IsInstanceOfType(instance));
            if (result != null)
            {
                return result;
            }

            return ServiceProvider.GetService(serviceType);
        }

        public IEnumerable<T> GetServices<T>()
        {
            var services = new List<T>();
            var results = _instances.Values.Where(instance => (instance is T));
            foreach (var result in results)
            {
                services.Add((T)result);
            }

            services.AddRange(ServiceProvider.GetServices<T>());
            return services;
        }

        public void Register(string name, object instance)
        {
            _ = _instances.AddOrUpdate(name, instance, (k, v) => instance);
        }

        public object Deregister(string name)
        {
            _instances.TryRemove(name, out var instance);

            if (instance is IDisposable)
            {
                ((IDisposable)instance).Dispose();
            }

            return instance;
        }
    }
}
