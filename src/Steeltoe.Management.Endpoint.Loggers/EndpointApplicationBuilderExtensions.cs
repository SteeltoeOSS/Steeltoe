using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Steeltoe.Management.Endpoint.Loggers
{
    public static class EndpointApplicationBuilderExtensions
    {
        public static void UseLoggersActuator(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.UseMiddleware<LoggersEndpointMiddleware>();
        }
    }
}
