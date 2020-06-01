// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Owin;
using System;

namespace Steeltoe.Management.EndpointOwin.Diagnostics
{
    public static class DiagnosticSourceAppBuilderExtensions
    {
        public static IAppBuilder UseDiagnosticSourceMiddleware(this IAppBuilder builder, ILoggerFactory loggerFactory = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var logger = loggerFactory?.CreateLogger<DiagnosticSourceOwinMiddleware>();
            return builder.Use<DiagnosticSourceOwinMiddleware>(logger);
        }
    }
}
