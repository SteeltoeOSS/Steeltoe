// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint;

internal static class EndpointOptionsExtensions
{
    public static bool IsEnabled(this EndpointOptions endpointOptions, ManagementOptions managementOptions)
    {
        ArgumentNullException.ThrowIfNull(endpointOptions);
        ArgumentNullException.ThrowIfNull(managementOptions);

        return endpointOptions.Enabled ?? managementOptions.Enabled;
    }

    public static bool IsExposed(this EndpointOptions endpointOptions, ManagementOptions managementOptions)
    {
        ArgumentNullException.ThrowIfNull(endpointOptions);
        ArgumentNullException.ThrowIfNull(managementOptions);

        if (!string.IsNullOrEmpty(endpointOptions.Id))
        {
            if (managementOptions.Exposure.Exclude.Contains("*") || managementOptions.Exposure.Exclude.Contains(endpointOptions.Id))
            {
                return false;
            }

            if (managementOptions.Exposure.Include.Contains("*") || managementOptions.Exposure.Include.Contains(endpointOptions.Id))
            {
                return true;
            }

            return false;
        }

        return true;
    }

    public static bool CanInvoke(this EndpointOptions endpointOptions, PathString requestPath, ManagementOptions managementOptions)
    {
        ArgumentNullException.ThrowIfNull(endpointOptions);
        ArgumentNullException.ThrowIfNull(managementOptions);

        bool isEnabled = IsEnabled(endpointOptions, managementOptions);
        bool isExposed = IsExposed(endpointOptions, managementOptions);

        bool isAtCloudFoundryPath = requestPath.StartsWithSegments(ConfigureManagementOptions.DefaultCloudFoundryPath);
        return isAtCloudFoundryPath ? managementOptions.IsCloudFoundryEnabled && isEnabled : isEnabled && isExposed;
    }

    public static string GetPathMatchPattern(this EndpointOptions endpointOptions, string? basePath)
    {
        ArgumentNullException.ThrowIfNull(endpointOptions);

        string path = GetEndpointPath(endpointOptions, basePath);

        if (!endpointOptions.RequiresExactMatch())
        {
            if (!path.EndsWith('/'))
            {
                path += '/';
            }

            path += "{**_}";
        }

        return path;
    }

    public static string GetEndpointPath(this EndpointOptions endpointOptions, string? basePath)
    {
        ArgumentNullException.ThrowIfNull(endpointOptions);

        string baseSegment = basePath == null ? string.Empty : basePath.Trim('/');
        string endpointSegment = endpointOptions.Path == null ? string.Empty : endpointOptions.Path.Trim('/');

        if (baseSegment.Length == 0)
        {
            return endpointSegment.Length == 0 ? "/" : $"/{endpointSegment}";
        }

        return endpointSegment.Length == 0 ? $"/{baseSegment}" : $"/{baseSegment}/{endpointSegment}";
    }
}
