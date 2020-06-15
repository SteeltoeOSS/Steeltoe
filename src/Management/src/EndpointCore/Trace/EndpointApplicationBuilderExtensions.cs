// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using System;

namespace Steeltoe.Management.Endpoint.Trace
{
    public static class EndpointApplicationBuilderExtensions
    {
        /// <summary>
        /// Enable the trace middleware
        /// </summary>
        /// <param name="builder">Your application builder</param>
        public static void UseTraceActuator(this IApplicationBuilder builder)
        {
            builder.UseTraceActuator(MediaTypeVersion.V1);
        }

        public static void UseTraceActuator(this IApplicationBuilder builder, MediaTypeVersion version)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            switch (version)
            {
                case MediaTypeVersion.V1:
                    builder.UseMiddleware<TraceEndpointMiddleware>();
                    break;
                default:
                    builder.UseMiddleware<HttpTraceEndpointMiddleware>();
                    break;
            }
        }
    }
}
