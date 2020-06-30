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
