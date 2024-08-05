// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint;

internal static class ManagementOptionsExtensions
{
    public static string? GetBaseRequestPath(this ManagementOptions managementOptions, HttpRequest httpRequest)
    {
        ArgumentNullException.ThrowIfNull(managementOptions);
        ArgumentNullException.ThrowIfNull(httpRequest);

        return httpRequest.Path.StartsWithSegments(ConfigureManagementOptions.DefaultCloudFoundryPath)
            ? ConfigureManagementOptions.DefaultCloudFoundryPath
            : managementOptions.Path;
    }
}
