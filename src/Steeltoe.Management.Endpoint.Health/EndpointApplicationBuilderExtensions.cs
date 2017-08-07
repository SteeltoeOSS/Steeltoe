using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;


namespace Steeltoe.Management.Endpoint.Health
{
    public static class EndpointApplicationBuilderExtensions
    {
        public static void UseHealthActuator(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.UseMiddleware<HealthEndpointMiddleware>();
        }
    }
}
