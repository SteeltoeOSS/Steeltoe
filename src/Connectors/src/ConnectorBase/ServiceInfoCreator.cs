﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
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
        private static IConfiguration _config;
        private static ServiceInfoCreator _me = null;
        private static object _lock = new object();

        internal ServiceInfoCreator(IConfiguration config)
        {
#pragma warning disable S3010 // Static fields should not be updated in constructors
            _config = config;
#pragma warning restore S3010 // Static fields should not be updated in constructors
            BuildServiceInfoFactories();
            BuildServiceInfos();
        }

        public IList<IServiceInfo> ServiceInfos { get; } = new List<IServiceInfo>();

        internal IList<IServiceInfoFactory> Factories { get; } = new List<IServiceInfoFactory>();

        public static ServiceInfoCreator Instance(IConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (config == _config)
            {
                return _me;
            }

            lock (_lock)
            {
                if (config == _config)
                {
                    return _me;
                }

                _me = new ServiceInfoCreator(config);
            }

            return _me;
        }

        /// <summary>
        /// Get all Service Infos of type
        /// </summary>
        /// <typeparam name="SI">Service Info Type to retrieve</typeparam>
        /// <returns>List of matching Service Infos</returns>
        public List<SI> GetServiceInfos<SI>()
            where SI : class
        {
            var results = new List<SI>();
            foreach (var info in ServiceInfos)
            {
                if (info is SI si)
                {
                    results.Add(si);
                }
            }

            return results;
        }

        /// <summary>
        /// Get all Service Infos of type
        /// </summary>
        /// <param name="type">Service Info Type to retrieve</param>
        /// <returns>List of matching Service Infos</returns>
        public List<IServiceInfo> GetServiceInfos(Type type)
        {
            return ServiceInfos.Where((info) => info.GetType() == type).ToList();
        }

        /// <summary>
        /// Get a named service
        /// </summary>
        /// <typeparam name="SI">Service Info type</typeparam>
        /// <param name="name">Service name</param>
        /// <returns>Service info or null</returns>
        public SI GetServiceInfo<SI>(string name)
            where SI : class
        {
            var typed = GetServiceInfos<SI>();
            foreach (var si in typed)
            {
                var info = si as IServiceInfo;
                if (info.Id.Equals(name))
                {
                    return (SI)info;
                }
            }

            return null;
        }

        /// <summary>
        /// Get a named Service Info
        /// </summary>
        /// <param name="name">Name of service info</param>
        /// <returns>Service info</returns>
        public IServiceInfo GetServiceInfo(string name)
        {
            return ServiceInfos.FirstOrDefault((info) => info.Id.Equals(name));
        }

        internal void BuildServiceInfoFactories()
        {
            Factories.Clear();

            var assembly = GetType().GetTypeInfo().Assembly;
            var types = assembly.DefinedTypes;
            foreach (var type in types)
            {
                if (type.IsDefined(typeof(ServiceInfoFactoryAttribute)))
                {
                    var instance = CreateServiceInfoFactory(type.DeclaredConstructors);
                    if (instance != null)
                    {
                        Factories.Add(instance);
                    }
                }
            }
        }

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

        internal IServiceInfoFactory FindFactory(Service s)
        {
            foreach (var f in Factories)
            {
                if (f.Accept(s))
                {
                    return f;
                }
            }

            return null;
        }

        private void BuildServiceInfos()
        {
            ServiceInfos.Clear();

            var appInfo = new ApplicationInstanceInfo(_config);
            var serviceOpts = new ServicesOptions(_config);

            foreach (var serviceopt in serviceOpts.Services)
            {
                foreach (var s in serviceopt.Value)
                {
                    var factory = FindFactory(s);
                    if (factory != null && factory.Create(s) is ServiceInfo info)
                    {
                        info.ApplicationInfo = appInfo;
                        ServiceInfos.Add(info);
                    }
                }
            }
        }
    }
}
