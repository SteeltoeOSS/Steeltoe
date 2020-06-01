// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using System;

namespace Steeltoe.Management.Endpoint.ThreadDump
{
    public static class EndpointApplicationBuilderExtensions
    {
        /// <summary>
        /// Enable the thread dump middleware
        /// </summary>
        /// <param name="builder">Your application builder</param>
        public static void UseThreadDumpActuator(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.UseThreadDumpActuator(MediaTypeVersion.V1);
        }

        /// <summary>
        /// Enable the thread dump middleware
        /// </summary>
        /// <param name="builder">Your application builder</param>
        /// <param name="version">MediaType version</param>
        public static void UseThreadDumpActuator(this IApplicationBuilder builder, MediaTypeVersion version)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            switch (version)
            {
                case MediaTypeVersion.V1:
                    builder.UseMiddleware<ThreadDumpEndpointMiddleware>();
                    break;
                default:
                    builder.UseMiddleware<ThreadDumpEndpointMiddleware_v2>();
                    break;
            }
        }
    }
}
