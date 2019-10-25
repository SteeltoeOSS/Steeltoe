// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Steeltoe.CloudFoundry.Connector.App;
using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Steeltoe.CloudFoundry.Connector
{
    public class CloudFoundryServiceInfoCreator
    {
        private static IConfiguration _config;
        private static CloudFoundryServiceInfoCreator _me = null;
        private static object _lock = new object();

        internal CloudFoundryServiceInfoCreator(IConfiguration config)
        {
#pragma warning disable S3010 // Static fields should not be updated in constructors
            _config = config;
#pragma warning restore S3010 // Static fields should not be updated in constructors
            BuildServiceInfoFactories();
            BuildServiceInfos();
        }

        public IList<IServiceInfo> ServiceInfos { get; } = new List<IServiceInfo>();

        internal IList<IServiceInfoFactory> Factories { get; } = new List<IServiceInfoFactory>();

        public static CloudFoundryServiceInfoCreator Instance(IConfiguration config)
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

                _me = new CloudFoundryServiceInfoCreator(config);
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
            List<SI> results = new List<SI>();
            foreach (IServiceInfo info in ServiceInfos)
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
            List<SI> typed = GetServiceInfos<SI>();
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
                    IServiceInfoFactory instance = CreateServiceInfoFactory(type.DeclaredConstructors);
                    if (instance != null)
                    {
                        Factories.Add(instance);
                    }
                }
            }
        }

        private IServiceInfoFactory CreateServiceInfoFactory(IEnumerable<ConstructorInfo> declaredConstructors)
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

        private void BuildServiceInfos()
        {
            ServiceInfos.Clear();

            CloudFoundryApplicationOptions appOpts = new CloudFoundryApplicationOptions();
            var aopSection = _config.GetSection(CloudFoundryApplicationOptions.CONFIGURATION_PREFIX);
            aopSection.Bind(appOpts);

            ApplicationInstanceInfo appInfo = new ApplicationInstanceInfo(appOpts);
            var serviceSection = _config.GetSection(CloudFoundryServicesOptions.CONFIGURATION_PREFIX);
            CloudFoundryServicesOptions serviceOpts = new CloudFoundryServicesOptions();
            serviceSection.Bind(serviceOpts);

            foreach (KeyValuePair<string, Service[]> serviceopt in serviceOpts.Services)
            {
                foreach (Service s in serviceopt.Value)
                {
                    IServiceInfoFactory factory = FindFactory(s);
                    if (factory != null && factory.Create(s) is ServiceInfo info)
                    {
                        info.ApplicationInfo = appInfo;
                        ServiceInfos.Add(info);
                    }
                }
            }
        }

        private IServiceInfoFactory FindFactory(Service s)
        {
            foreach (IServiceInfoFactory f in Factories)
            {
                if (f.Accept(s))
                {
                    return f;
                }
            }

            return null;
        }
    }
}
