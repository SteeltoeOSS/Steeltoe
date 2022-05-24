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

namespace Steeltoe.Management.Endpoint
{
    public static class EndPointExtensions
    {
        public static bool IsEnabled(this IEndpointOptions options, IManagementOptions mgmtOptions)
        {
            var endpointOptions = (AbstractEndpointOptions)options;

            if (endpointOptions.Enabled.HasValue)
            {
                return endpointOptions.Enabled.Value;
            }

            if (mgmtOptions.Enabled.HasValue)
            {
                return mgmtOptions.Enabled.Value;
            }

            return endpointOptions.DefaultEnabled;
        }

        public static bool IsEnabled(this IEndpoint endpoint, IManagementOptions mgmtContext)
        {
            return mgmtContext == null ? endpoint.Enabled : endpoint.Options.IsEnabled(mgmtContext);
        }

        public static bool IsExposed(this IEndpointOptions options, IManagementOptions mgmtOptions)
        {
            if (!string.IsNullOrEmpty(options.Id)
                && mgmtOptions is ActuatorManagementOptions actOptions
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

        public static bool IsExposed(this IEndpoint endpoint, IManagementOptions mgmtContext)
        {
            return mgmtContext == null || endpoint.Options.IsExposed(mgmtContext);
        }

        public static bool ShouldInvoke(this IEndpoint endpoint, IManagementOptions mgmtContext, ILogger logger = null)
        {
            var enabled = endpoint.IsEnabled(mgmtContext);
            var exposed = endpoint.IsExposed(mgmtContext);
            logger?.LogDebug($"endpoint: {endpoint.Id}, contextPath: {mgmtContext.Path}, enabled: {enabled}, exposed: {exposed}");
            return enabled && exposed;
        }

        public static IManagementOptions OptionsForContext(this IEnumerable<IManagementOptions> mgmtOptions, string requestPath, ILogger logger = null)
        {
            var match = mgmtOptions.FirstOrDefault(option => requestPath.StartsWith(option.Path));
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
                        return mgmtOptions.First(option => option is CloudFoundryManagementOptions);
                    }
                    else
                    {
                        return mgmtOptions.First(option => option is ActuatorManagementOptions);
                    }
                }
                catch (InvalidOperationException)
                {
                    logger?.LogError("Could not find IManagementOptions to match this request, returning first or default ActuatorManagementOptions");
                    return mgmtOptions.FirstOrDefault() ?? new ActuatorManagementOptions();
                }
            }
        }

        public static string GetContextPath(this IEndpointOptions options, IManagementOptions mgmtContext)
        {
            var contextPath = mgmtContext.Path;
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
}
