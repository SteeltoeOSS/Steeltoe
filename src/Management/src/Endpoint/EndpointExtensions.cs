// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint;

public static class EndPointExtensions
{
    public static bool IsEnabled(this IEndpointOptions options, ManagementEndpointOptions managementOptions)
    {
        var endpointOptions = (EndpointOptionsBase)options;

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

    public static bool IsExposed(this IEndpointOptions options, ManagementEndpointOptions mgmtOptions)
    {
        if (!string.IsNullOrEmpty(options.Id) && mgmtOptions.Exposure != null)
        {
            List<string> exclude = mgmtOptions.Exposure.Exclude;

            if (exclude != null && (exclude.Contains("*") || exclude.Contains(options.Id)))
            {
                return false;
            }

            List<string> include = mgmtOptions.Exposure.Include;

            if (include != null && (include.Contains("*") || include.Contains(options.Id)))
            {
                return true;
            }

            return false;
        }

        return true;
    }

    //public static bool IsExposed(this IEndpointOptions endpoint, ManagementEndpointOptions options)
    //{
    //    return options == null || endpoint.IsExposed(options);
    //}

    public static bool ShouldInvoke(this IEndpointOptions endpoint, ManagementEndpointOptions options, ILogger logger = null)
    {
        bool enabled = endpoint.IsEnabled(options);
        bool exposed = endpoint.IsExposed(options);
        logger?.LogDebug($"endpoint: {endpoint.Id}, contextPath: {options.Path}, enabled: {enabled}, exposed: {exposed}");
        return enabled && exposed;
    }

    public static bool ShouldInvoke(this IEndpointOptions endpoint, IOptionsMonitor<ManagementEndpointOptions> managementOptions, HttpContext context, ILogger logger = null)
    {
        ManagementEndpointOptions mgmtOptions = managementOptions.GetCurrentContext(context.Request.Path);

        return ShouldInvoke(endpoint, mgmtOptions, logger);
    }

    public static ManagementEndpointOptions GetCurrentContext(this IOptionsMonitor<ManagementEndpointOptions> managementOptions, PathString path)
    {
        List<ManagementEndpointOptions> options = new();
        foreach (string name in managementOptions.CurrentValue.ContextNames)
        {
            options.Add(managementOptions.Get(name));
        }
        options = options.OrderByDescending(option => option.Path.Length).ToList();
        foreach (ManagementEndpointOptions opt in options)
        {
            if (path.StartsWithSegments(new PathString(opt.Path)))
            {
                return opt;
            }
        }
        return managementOptions.Get(ActuatorContext.Name);
    }

    public static string GetContextPath(this IEndpointOptions options, ManagementEndpointOptions managementOptions)
    {
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

    //Only used by Cloudfoundry security
    public static bool IsAccessAllowed(this IEndpointOptions options, Permissions permissions)
    {
        return permissions >= options.RequiredPermissions;
    }
}
