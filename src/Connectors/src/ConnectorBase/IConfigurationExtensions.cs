// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Connector
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
            var factory = GetServiceInfoCreator(config);
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
            var factory = GetServiceInfoCreator(config);
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
            var factory = GetServiceInfoCreator(config);
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
            var factory = GetServiceInfoCreator(config);
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
            var results = GetServiceInfos<SI>(config);
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

        public static IConfigurationBuilder AddConnectionStrings(this IConfigurationBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Add(new ConnectionStringConfigurationSource(builder.Sources));
            return builder;
        }

        internal static ServiceInfoCreator GetServiceInfoCreator(IConfiguration config)
        {
            var alternateInfoCreators = ReflectionHelpers.FindTypeFromAssemblyAttribute<ServiceInfoCreatorAssemblyAttribute>();
            foreach (var alternateInfoCreator in alternateInfoCreators)
            {
                if ((bool)alternateInfoCreator.GetProperty("IsRelevant").GetValue(null))
                {
                    return (ServiceInfoCreator)alternateInfoCreator.GetMethod("Instance").Invoke(null, new[] { config });
                }
            }

            return ServiceInfoCreator.Instance(config);
        }
    }
}
