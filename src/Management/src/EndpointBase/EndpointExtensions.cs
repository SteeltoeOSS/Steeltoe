// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Endpoint;

public static class EndPointExtensions
{
    public static bool IsEnabled(this IEndpointOptions options, IManagementOptions managementOptions)
    {
        var endpointOptions = (AbstractEndpointOptions)options;

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

    public static bool IsEnabled(this IEndpoint endpoint, IManagementOptions options)
    {
        return options == null ? endpoint.Enabled : endpoint.Options.IsEnabled(options);
    }

    public static bool IsExposed(this IEndpointOptions options, IManagementOptions managementOptions)
    {
        if (!string.IsNullOrEmpty(options.Id)
            && managementOptions is ActuatorManagementOptions actOptions
            && actOptions.Exposure != null)
        {
            var exclude = actOptions.Exposure.Exclude;
            if (exclude != null && (exclude.Contains("*") || exclude.Contains(options.Id)))
            {
                return false;
            }

            var include = actOptions.Exposure.Include;
            if (include != null && (include.Contains("*") || include.Contains(options.Id)))
            {
                return true;
            }

            return false;
        }

        return true;
    }

    public static bool IsExposed(this IEndpoint endpoint, IManagementOptions options)
    {
        return options == null || endpoint.Options.IsExposed(options);
    }

    public static bool ShouldInvoke(this IEndpoint endpoint, IManagementOptions options, ILogger logger = null)
    {
        var enabled = endpoint.IsEnabled(options);
        var exposed = endpoint.IsExposed(options);
        logger?.LogDebug($"endpoint: {endpoint.Id}, contextPath: {options.Path}, enabled: {enabled}, exposed: {exposed}");
        return enabled && exposed;
    }

    public static IManagementOptions OptionsForContext(this IEnumerable<IManagementOptions> managementOptions, string requestPath, ILogger logger = null)
    {
        var match = managementOptions.FirstOrDefault(option => requestPath.StartsWith(option.Path));
        if (match != null)
        {
            logger?.LogTrace("Request path matched base path owned by {optionsType}", match.GetType().Name);
            return match;
        }
        else
        {
            try
            {
                if (Platform.IsCloudFoundry)
                {
                    return managementOptions.First(option => option is CloudFoundryManagementOptions);
                }
                else
                {
                    return managementOptions.First(option => option is ActuatorManagementOptions);
                }
            }
            catch (InvalidOperationException)
            {
                logger?.LogError("Could not find IManagementOptions to match this request, returning first or default ActuatorManagementOptions");
                return managementOptions.FirstOrDefault() ?? new ActuatorManagementOptions();
            }
        }
    }

    public static string GetContextPath(this IEndpointOptions options, IManagementOptions managementOptions)
    {
        var contextPath = managementOptions.Path;
        if (!contextPath.EndsWith("/") && !string.IsNullOrEmpty(options.Path))
        {
            contextPath += "/";
        }

        contextPath += options.Path;

        if (!options.ExactMatch)
        {
            if (!contextPath.EndsWith("/"))
            {
                contextPath += "/";
            }

            contextPath += "{**_}";
        }

        return contextPath;
    }
}
