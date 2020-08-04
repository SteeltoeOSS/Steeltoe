// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;
using Steeltoe.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Steeltoe.Connector
{
    public class ServiceInfoCreator
    {
        private static readonly object _lock = new object();
        private static ServiceInfoCreator _me;

        protected internal IConfiguration Configuration { get; }

        protected ServiceInfoCreator(IConfiguration configuration) => Configuration = configuration;

        /// <summary>
        /// Gets a value indicating whether this ServiceInfoCreator should be used
        /// </summary>
        public static bool IsRelevant { get; } = true;

        /// <summary>
        /// Gets a list of <see cref="IServiceInfo"/> that are configured in the applicaiton configuration
        /// </summary>
        public IList<IServiceInfo> ServiceInfos { get; } = new List<IServiceInfo>();

        /// <summary>
        /// Gets a list of <see cref="IServiceInfoFactory"/> available for finding <see cref="IServiceInfo"/>s
        /// </summary>
        protected internal IList<IServiceInfoFactory> Factories { get; } = new List<IServiceInfoFactory>();

        public static ServiceInfoCreator Instance(IConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (config == _me?.Configuration)
            {
                return _me;
            }

            lock (_lock)
            {
                if (config != _me?.Configuration)
                {
                    _me = new ServiceInfoCreator(config);
                    _me.BuildServiceInfoFactories();
                    _me.BuildServiceInfos();
                }
            }

            return _me;
        }

        /// <summary>
        /// Get all Service Infos of type
        /// </summary>
        /// <typeparam name="TServiceInfo">Service Info Type to retrieve</typeparam>
        /// <returns>List of matching Service Infos</returns>
        public IEnumerable<TServiceInfo> GetServiceInfos<TServiceInfo>()
            where TServiceInfo : class
                => ServiceInfos.Where(si => si is TServiceInfo).Cast<TServiceInfo>();

        /// <summary>
        /// Get all Service Infos of type
        /// </summary>
        /// <param name="type">Service Info Type to retrieve</param>
        /// <returns>List of matching Service Infos</returns>
        public IEnumerable<IServiceInfo> GetServiceInfos(Type type)
            => ServiceInfos.Where((info) => info.GetType() == type);

        /// <summary>
        /// Get a named service
        /// </summary>
        /// <typeparam name="TServiceInfo">Service Info type</typeparam>
        /// <param name="name">Service name</param>
        /// <returns>Service info or null</returns>
        public TServiceInfo GetServiceInfo<TServiceInfo>(string name)
            where TServiceInfo : class
        {
            var typed = GetServiceInfos<TServiceInfo>();
            foreach (var si in typed)
            {
                var info = si as IServiceInfo;
                if (info.Id.Equals(name))
                {
                    return (TServiceInfo)info;
                }
            }

            return null;
        }

        /// <summary>
        /// Get a named Service Info
        /// </summary>
        /// <param name="name">Name of service info</param>
        /// <returns>Service info</returns>
        public IServiceInfo GetServiceInfo(string name) => ServiceInfos.FirstOrDefault((info) => info.Id.Equals(name));

        internal IServiceInfoFactory CreateServiceInfoFactory(IEnumerable<ConstructorInfo> declaredConstructors)
        {
            IServiceInfoFactory result = null;
            foreach (var ci in declaredConstructors)
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

            var factories = ReflectionHelpers.FindTypesWithAttributeFromAssemblyAttribute<ServiceInfoFactoryAttribute, ServiceInfoFactoryAssemblyAttribute>();
            foreach (var type in factories)
            {
                var instance = CreateServiceInfoFactory(type.GetTypeInfo().DeclaredConstructors);
                if (instance != null)
                {
                    Factories.Add(instance);
                }
            }
        }

        protected IServiceInfoFactory FindFactory(Service s)
        {
            foreach (var f in Factories)
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

            var appInfo = new ApplicationInstanceInfo(Configuration);
            var serviceOpts = new ServicesOptions(Configuration);

            foreach (var service in serviceOpts.Services.SelectMany(s => s.Value))
            {
                var factory = FindFactory(service);
                if (factory != null && factory.Create(service) is ServiceInfo info)
                {
                    info.ApplicationInfo = appInfo;
                    ServiceInfos.Add(info);
                }
            }
        }
    }
}
