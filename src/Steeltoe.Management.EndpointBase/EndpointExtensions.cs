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

using System;
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

        public static bool IsSensitive(this IEndpointOptions options, IManagementOptions mgmtOptions)
        {
            var endpointOptions = (AbstractEndpointOptions)options;

            if (endpointOptions.Sensitive.HasValue)
            {
                return endpointOptions.Sensitive.Value;
            }

            if (mgmtOptions.Sensitive.HasValue)
            {
                return mgmtOptions.Sensitive.Value;
            }

            return endpointOptions.DefaultSensitive;
        }

        public static bool RequestVerbAndPathMatch(this IEndpoint endpoint, string httpMethod, string requestPath, IEnumerable<HttpMethod> allowedMethods, IEnumerable<IManagementOptions> mgmtOptions, bool exactMatch)
        {
            IManagementOptions matchingMgmtContext = null;
            return (exactMatch ? endpoint.RequestPathMatches(requestPath, mgmtOptions, out matchingMgmtContext)
                : endpoint.RequestPathStartsWith(requestPath, mgmtOptions, out matchingMgmtContext))
                && endpoint.IsEnabled(matchingMgmtContext)
                && allowedMethods.Any(m => m.Method.Equals(httpMethod));
        }

        public static bool IsEnabled(this IEndpoint endpoint, IManagementOptions mgmtContext)
        {
            return mgmtContext == null ? endpoint.Enabled : endpoint.Options.IsEnabled(mgmtContext);
        }

        public static bool RequestPathMatches(this IEndpoint endpoint, string requestPath, IEnumerable<IManagementOptions> mgmtOptions,  out IManagementOptions matchingContext)
        {
            matchingContext = null;

            if (mgmtOptions == null)
            {
                return requestPath.Equals(endpoint.Path);
            }
            else
            {
                foreach (var context in mgmtOptions)
                {
                    if (context.EndpointOptions.Any(opt => opt.Id == endpoint.Id))
                    {
                        var contextPath = context.Path;
                        if (!contextPath.EndsWith("/") && !string.IsNullOrEmpty(endpoint.Path))
                        {
                            contextPath += "/";
                        }

                        var fullPath = contextPath + endpoint.Path;
                        if (requestPath.Equals(fullPath))
                        {
                            matchingContext = context;
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public static bool RequestPathStartsWith(this IEndpoint endpoint, string requestPath, IEnumerable<IManagementOptions> mgmtOptions, out IManagementOptions matchingContext)
        {
            matchingContext = null;

            if (mgmtOptions == null)
            {
                return requestPath.StartsWith(endpoint.Path);
            }
            else
            {
                foreach (var context in mgmtOptions)
                {
                    if (context.EndpointOptions.Any(opt => opt.Id == endpoint.Id))
                    {
                        var contextPath = context.Path;
                        if (!contextPath.EndsWith("/") && !string.IsNullOrEmpty(endpoint.Path))
                        {
                            contextPath += "/";
                        }

                        var fullPath = contextPath + endpoint.Path;
                        if (requestPath.StartsWith(fullPath))
                        {
                            matchingContext = context;
                            return true;
                        }
                    }
                }

                return false;
            }
        }
    }
}
