using Microsoft.AspNetCore.Builder;
using System;


namespace Steeltoe.Management.Endpoint.Info
{
    public static class EndpointApplicationBuilderExtensions
    {
        public static void UseInfoActuator(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.UseMiddleware<InfoEndpointMiddleware>();
        }
    }
}
