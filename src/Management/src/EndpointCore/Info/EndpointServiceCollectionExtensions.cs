// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Info.Contributor;
using Steeltoe.Management.Info;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Info
{
    public static class EndpointServiceCollectionExtensions
    {
        /// <summary>
        /// Adds components of the Info actuator to Microsoft-DI
        /// </summary>
        /// <param name="services">Service collection to add info to</param>
        /// <param name="config">Application configuration (this actuator looks for a settings starting with management:endpoints:info)</param>
        public static void AddInfoActuator(this IServiceCollection services, IConfiguration config = null)
        {
            var serviceProvider = services.BuildServiceProvider();
            config ??= serviceProvider.GetRequiredService<IConfiguration>();
            var otherInfoContributors = serviceProvider.GetServices<IInfoContributor>();
            var allContributors = new List<IInfoContributor> { new GitInfoContributor(), new AppSettingsInfoContributor(config), new BuildInfoContributor() };
            foreach (var o in otherInfoContributors)
            {
                allContributors.Add(o);
            }

            services.AddInfoActuator(config, allContributors.ToArray());
        }

        /// <summary>
        /// Adds components of the info actuator to Microsoft-DI
        /// </summary>
        /// <param name="services">Service collection to add info to</param>
        /// <param name="config">Application configuration (this actuator looks for a settings starting with management:endpoints:info)</param>
        /// <param name="contributors">Contributors to application information</param>
        public static void AddInfoActuator(this IServiceCollection services, IConfiguration config, params IInfoContributor[] contributors)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IManagementOptions>(new ActuatorManagementOptions(config)));
            var options = new InfoEndpointOptions(config);
            services.TryAddSingleton<IInfoOptions>(options);
            services.RegisterEndpointOptions(options);
            AddContributors(services, contributors);
            services.TryAddSingleton<InfoEndpoint>();
        }

        private static void AddContributors(IServiceCollection services, params IInfoContributor[] contributors)
        {
            var descriptors = new List<ServiceDescriptor>();
            foreach (var instance in contributors)
            {
                descriptors.Add(ServiceDescriptor.Singleton(instance));
            }

            services.TryAddEnumerable(descriptors);
        }
    }
}
