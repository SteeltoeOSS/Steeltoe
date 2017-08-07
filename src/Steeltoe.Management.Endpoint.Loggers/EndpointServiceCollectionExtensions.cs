using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;



namespace Steeltoe.Management.Endpoint.Loggers
{
    public static class EndpointServiceCollectionExtensions
    {
        public static void AddLoggersActuator(this IServiceCollection services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            services.TryAddSingleton<ILoggersOptions>(new LoggersOptions(config));
    
            services.TryAddSingleton<LoggersEndpoint>();
        }

  
    }
}
