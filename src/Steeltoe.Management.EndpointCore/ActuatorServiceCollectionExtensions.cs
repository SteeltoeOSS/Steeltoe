using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Discovery;
using Steeltoe.Management.Endpoint.Env;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Mappings;
using Steeltoe.Management.Endpoint.Refresh;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Endpoint
{
    public static class ActuatorServiceCollectionExtensions
    {
        public static void AddDiscoveryActuators(this IServiceCollection services, IConfiguration config)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IManagementOptions>(new ActuatorManagementOptions(config)));
            services.AddDiscoveryActuator(config);
            services.AddInfoActuator(config, true);
            services.AddHealthActuator(config, true);
        }

        public static void AddAllDiscoveryActuators(this IServiceCollection services, IConfiguration config)
        {
            services.AddDiscoveryActuators(config);
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                services.AddThreadDumpActuator(config, true);
                services.AddHeapDumpActuator(config, true);
            }

            services.AddLoggersActuator(config, true);
            services.AddTraceActuator(config, true);
            services.AddMappingsActuator(config, true);
            services.AddEnvActuator(config, true);
            services.AddRefreshActuator(config, true);
            services.AddMappingsActuator(config, true);
        }

        public static void RegisterEndpointOptions(this IServiceCollection services, IEndpointOptions options, bool addToDiscovery)
        {
            var mgmtOptions = services.BuildServiceProvider().GetServices<IManagementOptions>();
            foreach (var mgmt in mgmtOptions)
            {
                if (!addToDiscovery && mgmt is ActuatorManagementOptions)
                {
                    continue;
                }

                mgmt.EndpointOptions.Add(options);
            }
        }
    }
}
