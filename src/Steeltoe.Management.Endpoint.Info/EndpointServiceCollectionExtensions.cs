using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using Steeltoe.Management.Endpoint.Info.Contributor;


namespace Steeltoe.Management.Endpoint.Info
{
    public static class EndpointServiceCollectionExtensions
    {
        public static void AddInfoActuator(this IServiceCollection services, IConfiguration config)
        {
            services.AddInfoActuator(config, new GitInfoContributor(), new AppSettingsInfoContributor(config));
        }

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
            services.TryAddSingleton<IInfoOptions>(new InfoOptions(config));
            AddContributors(services, contributors);
            services.TryAddSingleton<InfoEndpoint>();
        }

        private static void AddContributors(IServiceCollection services, params IInfoContributor[] contributors)
        {
            List<ServiceDescriptor> descriptors = new List<ServiceDescriptor>();
            foreach (var instance in contributors)
            {
                descriptors.Add(ServiceDescriptor.Singleton<IInfoContributor>(instance));
            }

            services.TryAddEnumerable(descriptors);
        }
    }
}
