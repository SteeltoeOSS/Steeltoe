// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Management.Endpoint.Hypermedia;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

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

        public static bool IsExposed(this IEndpointOptions options, IManagementOptions mgmtOptions)
        {
            var actOptions = mgmtOptions as ActuatorManagementOptions;

            if (!string.IsNullOrEmpty(options.Id) && actOptions != null && actOptions.Exposure != null)
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

        public static bool RequestVerbAndPathMatch(this IEndpoint endpoint, string httpMethod, string requestPath, IEnumerable<HttpMethod> allowedMethods, IEnumerable<IManagementOptions> mgmtOptions, bool exactMatch)
        {
            IManagementOptions matchingMgmtContext = null;
            return endpoint.RequestPathMatches(requestPath, mgmtOptions, out matchingMgmtContext, exactMatch)
                && endpoint.IsEnabled(matchingMgmtContext)
                && endpoint.IsExposed(matchingMgmtContext)
                && allowedMethods.Any(m => m.Method.Equals(httpMethod));
        }

        public static bool IsEnabled(this IEndpoint endpoint, IManagementOptions mgmtContext)
        {
            return mgmtContext == null ? endpoint.Enabled : endpoint.Options.IsEnabled(mgmtContext);
        }

        public static bool IsExposed(this IEndpoint endpoint, IManagementOptions mgmtContext)
        {
            return mgmtContext == null ? true : endpoint.Options.IsExposed(mgmtContext);
        }

        private static bool RequestPathMatches(this IEndpoint endpoint, string requestPath, IEnumerable<IManagementOptions> mgmtOptions, out IManagementOptions matchingContext, bool exactMatch = true)
        {
            matchingContext = null;
            var endpointPath = endpoint.Path;

            if (mgmtOptions == null)
            {
                return exactMatch ? requestPath.Equals(endpointPath) : requestPath.StartsWith(endpointPath);
            }
            else
            {
                foreach (var context in mgmtOptions)
                {
                    var contextPath = context.Path;
                    if (!contextPath.EndsWith("/") && !string.IsNullOrEmpty(endpointPath))
                    {
                        contextPath += "/";
                    }

                    var fullPath = contextPath + endpointPath;
                    if (exactMatch ? requestPath.Equals(fullPath) : requestPath.StartsWith(fullPath))
                    {
                        matchingContext = context;
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
