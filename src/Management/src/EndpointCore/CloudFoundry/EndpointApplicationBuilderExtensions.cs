// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using System;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

public static class EndpointApplicationBuilderExtensions
{
    /// <summary>
    /// Add CloudFoundry Security Middleware
    /// </summary>
    /// <param name="builder">Your application builder</param>
    public static void UseCloudFoundrySecurity(this IApplicationBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.UseMiddleware<CloudFoundrySecurityMiddleware>();
    }
}