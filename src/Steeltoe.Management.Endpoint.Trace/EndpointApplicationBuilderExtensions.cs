using Microsoft.AspNetCore.Builder;
using System;


namespace Steeltoe.Management.Endpoint.Trace
{
    public static class EndpointApplicationBuilderExtensions
    {
        public static void UseTraceActuator(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.UseMiddleware<TraceEndpointMiddleware>();
        }
    }
}
