// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint;

internal static class EndPointExtensions
{
    public static bool IsEnabled(this IHttpMiddlewareOptions options, ManagementEndpointOptions managementOptions)
    {
        ArgumentGuard.NotNull(managementOptions);

        var endpointOptions = (HttpMiddlewareOptions)options;

        if (endpointOptions.Enabled.HasValue)
        {
            return endpointOptions.Enabled.Value;
        }

        if (managementOptions.Enabled.HasValue)
        {
            return managementOptions.Enabled.Value;
        }

        return endpointOptions.DefaultEnabled;
    }

    public static bool IsExposed(this IHttpMiddlewareOptions options, ManagementEndpointOptions mgmtOptions)
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

    public static bool ShouldInvoke(this IHttpMiddlewareOptions endpoint, ManagementEndpointOptions options, ILogger logger)
    {
        ArgumentGuard.NotNull(logger);
        ArgumentGuard.NotNull(endpoint);
        ArgumentGuard.NotNull(options);

        bool enabled = endpoint.IsEnabled(options);
        bool exposed = endpoint.IsExposed(options);
        logger.LogDebug($"endpoint: {endpoint.Id}, contextPath: {options.Path}, enabled: {enabled}, exposed: {exposed}");
        return enabled && exposed;
    }

    public static bool ShouldInvoke(this IHttpMiddlewareOptions endpoint, IOptionsMonitor<ManagementEndpointOptions> managementOptions, HttpContext context,
        ILogger logger)
    {
        ArgumentGuard.NotNull(context);
        ManagementEndpointOptions mgmtOptions = managementOptions.GetFromContextPath(context.Request.Path);

        return ShouldInvoke(endpoint, mgmtOptions, logger);
    }

    public static ManagementEndpointOptions GetFromContextPath(this IOptionsMonitor<ManagementEndpointOptions> managementOptions, PathString path)
    {
        List<ManagementEndpointOptions> options = new();

        foreach (string name in managementOptions.CurrentValue.ContextNames)
        {
            options.Add(managementOptions.Get(name));
        }

        foreach (ManagementEndpointOptions opt in options)
        {
            if (path.StartsWithSegments(new PathString(opt.Path)))
            {
                return opt;
            }
        }

        return managementOptions.Get(ActuatorContext.Name);
    }

    public static string GetContextPath(this IHttpMiddlewareOptions options, ManagementEndpointOptions managementOptions)
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

    // Only used by Cloudfoundry security
    public static bool IsAccessAllowed(this IHttpMiddlewareOptions options, Permissions permissions)
    {
        ArgumentGuard.NotNull(options);
        return permissions >= options.RequiredPermissions;
    }
}
