// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Trace;

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

    public static bool IsExposed(this HttpMiddlewareOptions options, ManagementEndpointOptions mgmtOptions)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(mgmtOptions);

        if (!string.IsNullOrEmpty(options.Id) && mgmtOptions.Exposure != null)
        {
            IList<string> exclude = mgmtOptions.Exposure.Exclude;

            if (exclude != null && (exclude.Contains("*") || exclude.Contains(options.Id)))
            {
                return false;
            }

            IList<string> include = mgmtOptions.Exposure.Include;

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
        var defaultCFContextPath = ConfigureManagementEndpointOptions.DefaultCFPath;

        return httpRequest.Path.StartsWithSegments(defaultCFContextPath) ? defaultCFContextPath : $"{managementOptions.Path}";

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

        if (!options.ExactMatch)
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
