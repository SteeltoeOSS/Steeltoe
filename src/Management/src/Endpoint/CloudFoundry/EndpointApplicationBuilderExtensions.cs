// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

public static class EndpointApplicationBuilderExtensions
{
    /// <summary>
    /// Add CloudFoundry Security Middleware.
    /// </summary>
    /// <param name="builder">
    /// Your application builder.
    /// </param>
    public static void UseCloudFoundrySecurity(this IApplicationBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        if (builder.ApplicationServices.GetService<PermissionsProvider>() == null)
        {
            throw new InvalidOperationException(
                $"Please call IServiceCollection.{nameof(CloudFoundrySecurityServiceCollectionExtensions.AddCloudFoundrySecurity)} first.");
        }

        builder.UseMiddleware<CloudFoundrySecurityMiddleware>();
    }
}
