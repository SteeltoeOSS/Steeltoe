// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Autofac;
using Autofac.Builder;
using Microsoft.Extensions.Configuration;
using Steeltoe.CloudFoundry.Connector.MySql;
using Steeltoe.CloudFoundry.Connector.Services;
using System;

namespace Steeltoe.CloudFoundry.Connector.EF6Autofac
{
    public static class MySqlDbContextContainerBuilderExtensions
    {
        /// <summary>
        /// Add your MySql-based DbContext to the ContainerBuilder
        /// </summary>
        /// <typeparam name="TContext">Your DbContext</typeparam>
        /// <param name="container">Autofac <see cref="ContainerBuilder" /></param>
        /// <param name="config">Your app config</param>
        /// <param name="serviceName">Name of service instance</param>
        /// <returns><see cref="IRegistrationBuilder{TLimit, TActivatorData, TRegistrationStyle}"/></returns>
        public static IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle> RegisterDbContext<TContext>(this ContainerBuilder container, IConfiguration config, string serviceName = null)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            MySqlServiceInfo info = serviceName == null
                ? config.GetSingletonServiceInfo<MySqlServiceInfo>()
                : config.GetRequiredServiceInfo<MySqlServiceInfo>(serviceName);

            var mySqlConfig = new MySqlProviderConnectorOptions(config);
            var factory = new MySqlProviderConnectorFactory(info, mySqlConfig, typeof(TContext));
            return container.Register(c => factory.Create(null)).As<TContext>();
        }
    }
}