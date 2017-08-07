using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;



namespace Steeltoe.Management.Endpoint.Trace
{
    public static class EndpointServiceCollectionExtensions
    {


        public static void AddTraceActuator(this IServiceCollection services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            services.TryAddSingleton<ITraceOptions>(new TraceOptions(config));
            services.TryAddSingleton<ITraceRepository,TraceObserver>();
            services.TryAddSingleton<TraceEndpoint>();
        }

    }
}
