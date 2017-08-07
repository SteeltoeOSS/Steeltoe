using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Endpoint.CloudFoundry
{
    public static class EndpointApplicationBuilderExtensions
    {
        public static void UseCloudFoundryActuator(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
          
            builder.UseMiddleware<CloudFoundrySecurityMiddleware>();
            builder.UseMiddleware<CloudFoundryEndpointMiddleware>();

        }
    }
}
