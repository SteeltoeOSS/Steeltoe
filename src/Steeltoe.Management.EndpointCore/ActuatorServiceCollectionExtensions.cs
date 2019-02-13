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
        public static void RegisterEndpointOptions(this IServiceCollection services, IEndpointOptions options)
        {
            var mgmtOptions = services.BuildServiceProvider().GetServices<IManagementOptions>();
            foreach (var mgmt in mgmtOptions)
            {
                mgmt.EndpointOptions.Add(options);
            }
        }
    }
}
