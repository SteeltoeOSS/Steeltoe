// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Configuration;
using Steeltoe.Common.Expression.Internal.Contexts;
using Steeltoe.Common.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Common.Contexts
{
#pragma warning disable S3881 // "IDisposable" should be implemented correctly
    public abstract class AbstractApplicationContext : IApplicationContext
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
    {
        private readonly ConcurrentDictionary<string, object> _instances = new ConcurrentDictionary<string, object>();

        protected AbstractApplicationContext(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            ServiceProvider = serviceProvider;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; private set; }

        public IServiceProvider ServiceProvider { get; private set; }

        public IServiceExpressionResolver ServiceExpressionResolver { get; set; }

        public bool ContainsService(string name)
        {
            _instances.TryGetValue(name, out var instance);
            return instance != null;
        }

        public bool ContainsService(string name, Type serviceType)
        {
            if (_instances.TryGetValue(name, out var instance))
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

            if (found != null)
            {
                Register(((IServiceNameAware)found).ServiceName, found);
                return true;
            }

            return false;
        }

        public bool ContainsService<T>(string name)
        {
            return ContainsService(name, typeof(T));
        }

        public object GetService(string name)
        {
            _instances.TryGetValue(name, out var instance);
            return instance;
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

            if (found != null)
            {
                Register(((IServiceNameAware)found).ServiceName, found);
            }

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

            var found = ServiceProvider.GetService(serviceType);
            if (found is IServiceNameAware aware)
            {
                Register(aware.ServiceName, found);
            }

            return found;
        }

        public IEnumerable<T> GetServices<T>()
        {
            var services = new List<T>();
            var found = ServiceProvider.GetServices<T>();
            foreach (var service in found)
            {
                if (service is IServiceNameAware aware)
                {
                    Register(aware.ServiceName, service);
                }
                else
                {
                    services.Add(service);
                }
            }

            var results = _instances.Values.Where(instance => (instance is T));
            foreach (var result in results)
            {
                services.Add((T)result);
            }

            return services;
        }

        public void Register(string name, object instance)
        {
            _ = _instances.AddOrUpdate(name, instance, (k, v) => instance);
        }

        public object Deregister(string name)
        {
            _instances.TryRemove(name, out var instance);

            if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }

            return instance;
        }

        public string ResolveEmbeddedValue(string value)
        {
            if (value == null)
            {
                return null;
            }

            var resolved = PropertyPlaceholderHelper.ResolvePlaceholders(value, Configuration);
            return resolved.Trim();
        }

        public void Dispose()
        {
            _instances.Clear();
            Configuration = null;
            ServiceProvider = null;
            ServiceExpressionResolver = null;
        }
    }
}
