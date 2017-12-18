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

using Microsoft.Extensions.Configuration;
using Steeltoe.CloudFoundry.Connector.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.CloudFoundry.Connector
{
    public static class IConfigurationExtensions
    {
        /// <summary>
        /// Get configuration info for all services of a given service type
        /// </summary>
        /// <typeparam name="SI">Service info type you're looking for</typeparam>
        /// <param name="config">Configuration to search</param>
        /// <returns>List of service infos</returns>
        public static List<SI> GetServiceInfos<SI>(this IConfiguration config)
            where SI : class
        {
            CloudFoundryServiceInfoCreator factory = CloudFoundryServiceInfoCreator.Instance(config);
            return factory.GetServiceInfos<SI>();
        }

        /// <summary>
        /// Get configuration info for all services of a given service type
        /// </summary>
        /// <param name="config">Configuration to search</param>
        /// <param name="infoType">Type to search for</param>
        /// <returns>A list of relevant <see cref="IServiceInfo"/></returns>
        public static List<IServiceInfo> GetServiceInfos(this IConfiguration config, Type infoType)
        {
            CloudFoundryServiceInfoCreator factory = CloudFoundryServiceInfoCreator.Instance(config);
            return factory.GetServiceInfos(infoType);
        }

        /// <summary>
        /// Get service info when you know the Id
        /// </summary>
        /// <param name="config">Configuration to search</param>
        /// <param name="id">Id of service</param>
        /// <returns>Requested implementation of <see cref="IServiceInfo"/></returns>
        public static IServiceInfo GetServiceInfo(this IConfiguration config, string id)
        {
            CloudFoundryServiceInfoCreator factory = CloudFoundryServiceInfoCreator.Instance(config);
            return factory.GetServiceInfo(id);
        }

        /// <summary>
        /// Get service info of a given type when you know the Id
        /// </summary>
        /// <typeparam name="SI">Service info type you're looking for</typeparam>
        /// <param name="config">Configuration to search</param>
        /// <param name="id">Id of service</param>
        /// <returns>Requested implementation of <see cref="IServiceInfo"/></returns>
        public static SI GetServiceInfo<SI>(this IConfiguration config, string id)
            where SI : class
        {
            CloudFoundryServiceInfoCreator factory = CloudFoundryServiceInfoCreator.Instance(config);
            return factory.GetServiceInfo<SI>(id);
        }

        /// <summary>
        /// Get Service Info from IConfiguration
        /// </summary>
        /// <typeparam name="SI">Type of Service Info to return</typeparam>
        /// <param name="config">Configuration to retrieve service info from</param>
        /// <exception cref="ConnectorException">Thrown when multple matching services are found</exception>
        /// <returns>Information requried to connect to provisioned service</returns>
        public static SI GetSingletonServiceInfo<SI>(this IConfiguration config)
            where SI : class
        {
            List<SI> results = GetServiceInfos<SI>(config);
            if (results.Count > 0)
            {
                if (results.Count != 1)
                {
                    throw new ConnectorException(string.Format("Multiple services of type: {0}, bound to application.", typeof(SI)));
                }

                return results[0];
            }

            return null;
        }

        /// <summary>
        /// Get info for a named service
        /// </summary>
        /// <typeparam name="SI">Type of Service Info to return</typeparam>
        /// <param name="config">Configuration to retrieve service info from</param>
        /// <param name="serviceName">Name of the service</param>
        /// <exception cref="ConnectorException">Thrown when service info isn't found</exception>
        /// <returns>Information requried to connect to provisioned service</returns>
        public static SI GetRequiredServiceInfo<SI>(this IConfiguration config, string serviceName)
            where SI : class
        {
            var info = GetServiceInfo<SI>(config, serviceName);
            if (info == null)
            {
                throw new ConnectorException(string.Format("No service with name: {0} found.", serviceName));
            }

            return info;
        }

        /// <summary>
        /// Evaluate whether an IConfiguration contains services bound by Cloud Foundry
        /// </summary>
        /// <param name="config">Application Configuration</param>
        /// <returns>true if vcap:services found in config, othwerwise false</returns>
        public static bool HasCloudFoundryServiceConfigurations(this IConfiguration config)
        {
            return config.GetSection("vcap:services").GetChildren().Any();
        }
    }
}
