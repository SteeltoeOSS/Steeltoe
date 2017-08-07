using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;


namespace Steeltoe.Management.Endpoint.CloudFoundry
{
    public static class EndpointServiceCollectionExtensions
    {
        public static void AddCloudFoundryActuator(this IServiceCollection services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            services.TryAddSingleton<ICloudFoundryOptions>(new CloudFoundryOptions(config));
            services.TryAddSingleton<CloudFoundryEndpoint>();
        }

        //public static void AddCloudFoundryActuators(this IServiceCollection services, IConfiguration config)
        //{
        //    services.AddCloudFoundryActuator(config);
            
        //}
    }
}
