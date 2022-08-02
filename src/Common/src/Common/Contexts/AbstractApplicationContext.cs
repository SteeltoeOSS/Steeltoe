// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Configuration;
using Steeltoe.Common.Expression.Internal.Contexts;
using Steeltoe.Common.Services;

namespace Steeltoe.Common.Contexts;

public abstract class AbstractApplicationContext : IApplicationContext
{
    private readonly ConcurrentDictionary<string, object> _instances = new();

    public IConfiguration Configuration { get; private set; }

    public IServiceProvider ServiceProvider { get; private set; }

    public IServiceExpressionResolver ServiceExpressionResolver { get; set; }

    protected AbstractApplicationContext(IServiceProvider serviceProvider, IConfiguration configuration, IEnumerable<NameToTypeMapping> nameToTypeMappings)
    {
        ServiceProvider = serviceProvider;
        Configuration = configuration;

        if (nameToTypeMappings != null)
        {
            foreach (NameToTypeMapping seed in nameToTypeMappings)
            {
                Register(seed.Name, seed.Type);
            }
        }
    }

    public bool ContainsService(string name)
    {
        _instances.TryGetValue(name, out object instance);

        if (instance is Type type)
        {
            instance = ResolveNamedService(name, type);
        }

        return instance != null;
    }

    public bool ContainsService(string name, Type serviceType)
    {
        if (_instances.TryGetValue(name, out object instance))
        {
            if (instance is Type type)
            {
                instance = ResolveNamedService(name, type);
            }

            return serviceType.IsInstanceOfType(instance);
        }

        if (!typeof(IServiceNameAware).IsAssignableFrom(serviceType))
        {
            return false;
        }

        object found = FindNamedService(name, serviceType);

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
        _instances.TryGetValue(name, out object instance);

        if (instance is Type type)
        {
            instance = ResolveNamedService(name, type);
        }

        return instance;
    }

    public object GetService(string name, Type serviceType)
    {
        if (_instances.TryGetValue(name, out object instance))
        {
            if (instance is Type type)
            {
                instance = ResolveNamedService(name, type);
            }

            if (serviceType.IsInstanceOfType(instance))
            {
                return instance;
            }

            return null;
        }

        if (!typeof(IServiceNameAware).IsAssignableFrom(serviceType))
        {
            return null;
        }

        object found = FindNamedService(name, serviceType);

        if (found != null)
        {
            Register(((IServiceNameAware)found).ServiceName, found);
        }

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
        return (T)GetService(typeof(T));
    }

    public object GetService(Type serviceType)
    {
        object result = _instances.Values.LastOrDefault(serviceType.IsInstanceOfType);

        if (result != null)
        {
            return result;
        }

        object found = ServiceProvider.GetService(serviceType);

        if (found is IServiceNameAware aware)
        {
            Register(aware.ServiceName, found);
        }

        return found;
    }

    public IEnumerable<object> GetServices(Type serviceType)
    {
        var services = new List<object>();
        IEnumerable<object> found = ServiceProvider.GetServices(serviceType);

        foreach (object service in found)
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

        IEnumerable<object> results = _instances.Values.Where(serviceType.IsInstanceOfType);

        foreach (object result in results)
        {
            services.Add(result);
        }

        return services;
    }

    public IEnumerable<T> GetServices<T>()
    {
        var services = new List<T>();
        IEnumerable<T> found = ServiceProvider.GetServices<T>();

        foreach (T service in found)
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

        IEnumerable<object> results = _instances.Values.Where(instance => instance is T);

        foreach (object result in results)
        {
            services.Add((T)result);
        }

        return services;
    }

    public void Register(string name, object instance)
    {
        if (!string.IsNullOrEmpty(name))
        {
            _ = _instances.AddOrUpdate(name, instance, (_, _) => instance);
        }
    }

    public object Deregister(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        _instances.TryRemove(name, out object instance);

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

        string resolved = PropertyPlaceholderHelper.ResolvePlaceholders(value, Configuration);
        return resolved.Trim();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _instances.Clear();
            Configuration = null;
            ServiceProvider = null;
            ServiceExpressionResolver = null;
        }
    }

    private object ResolveNamedService(string name, Type serviceType)
    {
        object instance = FindNamedService(name, serviceType);

        if (instance != null)
        {
            Register(name, instance);
        }

        return instance;
    }

    private object FindNamedService(string name, Type serviceType)
    {
        object found = ServiceProvider.GetServices(serviceType).SingleOrDefault(service =>
        {
            if (service is IServiceNameAware nameAware)
            {
                return nameAware.ServiceName == name;
            }

            return false;
        });

        return found;
    }

    public class NameToTypeMapping
    {
        public string Name { get; }

        public Type Type { get; }

        public NameToTypeMapping(string name, Type type)
        {
            Name = name;
            Type = type;
        }
    }
}
