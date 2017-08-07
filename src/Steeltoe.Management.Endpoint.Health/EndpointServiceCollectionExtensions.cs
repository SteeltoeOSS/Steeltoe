using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management.Endpoint.Health.Contributor;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Steeltoe.Management.Endpoint.Health
{
    public static class EndpointServiceCollectionExtensions
    {
        public static void AddHealthActuator(this IServiceCollection services, IConfiguration config)
        {
            services.AddHealthActuator(config, new DefaultHealthAggregator(), GetDefaultContributors());
        }

        public static void AddHealthActuator(this IServiceCollection services, IConfiguration config, params IHealthContributor[] contributors)
        {
            services.AddHealthActuator(config, new DefaultHealthAggregator(), contributors);
        }

        public static void AddHealthActuator(this IServiceCollection services, IConfiguration config, IHealthAggregator aggregator, params IHealthContributor[] contributors)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            services.TryAddSingleton<IHealthOptions>(new HealthOptions(config));
            AddContributors(services, contributors);
            services.TryAddSingleton<IHealthAggregator>(aggregator);
            services.TryAddSingleton<HealthEndpoint>();

        }

        private static void AddContributors(IServiceCollection services, params IHealthContributor[] contributors)
        {
            List<ServiceDescriptor> descriptors = new List<ServiceDescriptor>();
            foreach (var instance in contributors)
            {
                descriptors.Add(ServiceDescriptor.Singleton<IHealthContributor>(instance));
            }

            services.TryAddEnumerable(descriptors);
        }

        private static IHealthContributor[] GetDefaultContributors()
        {
            return new IHealthContributor[]
            {
                new DiskSpaceContributor()
            };

        }
    }
}

