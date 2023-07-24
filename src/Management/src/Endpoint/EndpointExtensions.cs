// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint;

internal static class EndPointExtensions
{
    public static bool IsEnabled(this HttpMiddlewareOptions options, ManagementEndpointOptions managementOptions)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(managementOptions);

        if (options.Enabled.HasValue)
        {
            return options.Enabled.Value;
        }

        if (managementOptions.Enabled.HasValue)
        {
            return managementOptions.Enabled.Value;
        }

        return options.DefaultEnabled;
    }

    public static bool IsExposed(this HttpMiddlewareOptions options, ManagementEndpointOptions endpointOptions)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(endpointOptions);

        if (!string.IsNullOrEmpty(options.Id) && endpointOptions.Exposure != null)
        {
            IList<string> exclude = endpointOptions.Exposure.Exclude;

            if (exclude != null && (exclude.Contains("*") || exclude.Contains(options.Id)))
            {
                return false;
            }

            IList<string> include = endpointOptions.Exposure.Include;

            if (include != null && (include.Contains("*") || include.Contains(options.Id)))
            {
                return true;
            }

            return false;
        }

        return true;
    }

    public static string GetContextBasePath(this ManagementEndpointOptions managementOptions, HttpRequest httpRequest)
    {
        return httpRequest.Path.StartsWithSegments(ConfigureManagementEndpointOptions.DefaultCloudFoundryPath)
            ? ConfigureManagementEndpointOptions.DefaultCloudFoundryPath
            : $"{managementOptions.Path}";
    }

    public static string GetPathMatchPattern(this HttpMiddlewareOptions options, string contextBasePath, ManagementEndpointOptions managementOptions)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(managementOptions);

        string contextPath = contextBasePath;

        if (!contextPath.EndsWith('/') && !string.IsNullOrEmpty(options.Path))
        {
            contextPath += '/';
        }

        contextPath += options.Path;

        if (!options.RequiresExactMatch())
        {
            if (!contextPath.EndsWith('/'))
            {
                contextPath += '/';
            }

            contextPath += "{**_}";
        }

        return contextPath;
    }
}
