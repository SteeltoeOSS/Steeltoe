// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint;

internal static class EndpointOptionsExtensions
{
    public static bool IsEnabled(this EndpointOptions endpointOptions, ManagementOptions managementOptions)
    {
        ArgumentNullException.ThrowIfNull(endpointOptions);
        ArgumentNullException.ThrowIfNull(managementOptions);

        if (endpointOptions.Enabled != null)
        {
            return endpointOptions.Enabled.Value;
        }

        if (managementOptions.Enabled != null)
        {
            return managementOptions.Enabled.Value;
        }

        return endpointOptions.DefaultEnabled;
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

    public static string GetPathMatchPattern(this EndpointOptions endpointOptions, ManagementOptions managementOptions, string? baseRequestPath)
    {
        ArgumentNullException.ThrowIfNull(endpointOptions);
        ArgumentNullException.ThrowIfNull(managementOptions);

        string? path = baseRequestPath;

        if (path == null || (!path.EndsWith('/') && !string.IsNullOrEmpty(endpointOptions.Path)))
        {
            path += '/';
        }

        path += endpointOptions.Path;

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
}
