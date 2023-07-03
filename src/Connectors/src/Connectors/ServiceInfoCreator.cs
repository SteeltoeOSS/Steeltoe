// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Common.Reflection;
using Steeltoe.Configuration;
using Steeltoe.Connectors.Services;

namespace Steeltoe.Connectors;

public class ServiceInfoCreator
{
    private static readonly object Lock = new();
    private static ServiceInfoCreator _me;

    /// <summary>
    /// Gets a value indicating whether this ServiceInfoCreator should be used.
    /// </summary>
    public static bool IsRelevant { get; } = true;

    protected internal IConfiguration Configuration { get; }

    /// <summary>
    /// Gets a list of <see cref="IServiceInfoFactory" /> available for finding <see cref="IServiceInfo" />s.
    /// </summary>
    protected internal IList<IServiceInfoFactory> Factories { get; } = new List<IServiceInfoFactory>();

    /// <summary>
    /// Gets a list of <see cref="IServiceInfo" /> that are configured in the application configuration.
    /// </summary>
    public IList<IServiceInfo> ServiceInfos { get; } = new List<IServiceInfo>();

    protected ServiceInfoCreator(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public static ServiceInfoCreator Instance(IConfiguration configuration)
    {
        ArgumentGuard.NotNull(configuration);

        if (configuration != _me?.Configuration)
        {
            lock (Lock)
            {
                if (configuration != _me?.Configuration)
                {
                    _me = new ServiceInfoCreator(configuration);
                    _me.BuildServiceInfoFactories();
                    _me.BuildServiceInfos();
                }
            }
        }

        return _me;
    }

    /// <summary>
    /// Get all Service Infos of type.
    /// </summary>
    /// <typeparam name="TServiceInfo">
    /// Service Info Type to retrieve.
    /// </typeparam>
    /// <returns>
    /// List of matching Service Infos.
    /// </returns>
    public IEnumerable<TServiceInfo> GetServiceInfosOfType<TServiceInfo>()
        where TServiceInfo : class
    {
        return ServiceInfos.Where(si => si is TServiceInfo).Cast<TServiceInfo>();
    }

    /// <summary>
    /// Get all Service Infos of type.
    /// </summary>
    /// <param name="type">
    /// Service Info Type to retrieve.
    /// </param>
    /// <returns>
    /// List of matching Service Infos.
    /// </returns>
    public IEnumerable<IServiceInfo> GetServiceInfosOfType(Type type)
    {
        return ServiceInfos.Where(info => info.GetType() == type);
    }

    /// <summary>
    /// Get a named service.
    /// </summary>
    /// <typeparam name="TServiceInfo">
    /// Service Info type.
    /// </typeparam>
    /// <param name="name">
    /// Service name.
    /// </param>
    /// <returns>
    /// Service info or null.
    /// </returns>
    public TServiceInfo GetServiceInfo<TServiceInfo>(string name)
        where TServiceInfo : class
    {
        IEnumerable<TServiceInfo> typed = GetServiceInfosOfType<TServiceInfo>();

        foreach (TServiceInfo si in typed)
        {
            var info = si as IServiceInfo;

            if (info.Id == name)
            {
                return (TServiceInfo)info;
            }
        }

        return null;
    }

    /// <summary>
    /// Get a named Service Info.
    /// </summary>
    /// <param name="name">
    /// Name of service info.
    /// </param>
    /// <returns>
    /// Service info.
    /// </returns>
    public IServiceInfo GetServiceInfo(string name)
    {
        return ServiceInfos.FirstOrDefault(info => info.Id == name);
    }

    internal IServiceInfoFactory CreateServiceInfoFactory(IEnumerable<ConstructorInfo> declaredConstructors)
    {
        IServiceInfoFactory result = null;

        foreach (ConstructorInfo ci in declaredConstructors)
        {
            if (ci.GetParameters().Length == 0 && ci.IsPublic && !ci.IsStatic)
            {
                result = ci.Invoke(null) as IServiceInfoFactory;
                break;
            }
        }

        return result;
    }

    protected virtual void BuildServiceInfoFactories()
    {
        Factories.Clear();

        IEnumerable<Type> factories =
            ReflectionHelpers.FindTypesWithAttributeFromAssemblyAttribute<ServiceInfoFactoryAttribute, ServiceInfoFactoryAssemblyAttribute>();

        foreach (Type type in factories)
        {
            IServiceInfoFactory instance = CreateServiceInfoFactory(type.GetTypeInfo().DeclaredConstructors);

            if (instance != null)
            {
                Factories.Add(instance);
            }
        }
    }

    protected IServiceInfoFactory FindFactory(Service s)
    {
        foreach (IServiceInfoFactory f in Factories)
        {
            if (f.Accepts(s))
            {
                return f;
            }
        }

        return null;
    }

    private void BuildServiceInfos()
    {
        ServiceInfos.Clear();

        var appInfo = new ApplicationInstanceInfo(Configuration, true);
        var serviceOpts = new ServicesOptions(Configuration);

        foreach (Service service in serviceOpts.Services.SelectMany(s => s.Value))
        {
            IServiceInfoFactory factory = FindFactory(service);

            if (factory != null && factory.Create(service) is ServiceInfo info)
            {
                info.ApplicationInfo = appInfo;
                ServiceInfos.Add(info);
            }
        }
    }
}
