// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Web.Hypermedia;

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

    public static ManagementEndpointOptions GetFromContextPath(this IOptionsMonitor<ManagementEndpointOptions> managementOptions, PathString path,
        out EndpointContext endpointContext)
    {
        foreach (EndpointContext context in Enum.GetValues<EndpointContext>())
        {
            var options = managementOptions.Get(context);

            if (path.StartsWithSegments(new PathString(options.Path)))
            {
                endpointContext = context;
                return options;
            }
        }

        endpointContext = EndpointContext.Actuator;
        return managementOptions.Get(endpointContext);
    }

    public static string GetContextPath(this HttpMiddlewareOptions options, ManagementEndpointOptions managementOptions)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(managementOptions);

        string contextPath = managementOptions.Path;

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
