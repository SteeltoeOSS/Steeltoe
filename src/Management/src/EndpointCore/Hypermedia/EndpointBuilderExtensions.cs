using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management;
using Steeltoe.Management.Endpoint;

namespace Steeltoe.Management.Endpoint.Hypermedia
{
    public static class EndpointBuilderExtensions
    {
        public static IEndpointConventionBuilder MapActuator(
            this IEndpointRouteBuilder endpoints
        )
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

          
            var actuator = endpoints.ServiceProvider.GetService<ActuatorEndpoint>();
           // var middle = endpoints.ServiceProvider.GetService<ActuatorHypermediaEndpointHandler>();
            var mgmtOptions = 
                endpoints.ServiceProvider
                    .GetService<IEnumerable<IManagementOptions>>()
                    .OfType<ActuatorManagementOptions>().FirstOrDefault();


            var contextPath = mgmtOptions.Path;
            if (!contextPath.EndsWith("/") && !string.IsNullOrEmpty(actuator.Path))
            {
                contextPath += "/";
            }

            var fullPath = contextPath + actuator.Path;
            var pipeline = endpoints.CreateApplicationBuilder()
                .UseMiddleware<ActuatorHypermediaEndpointHandler>()
                .Build();

            return endpoints.Map(fullPath,pipeline);
        }

    }
}