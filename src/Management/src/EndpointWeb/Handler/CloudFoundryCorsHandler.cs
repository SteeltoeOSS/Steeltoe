﻿// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace Steeltoe.Management.Endpoint.Handler
{
    public class CloudFoundryCorsHandler : ActuatorHandler
    {
        private IEndpointOptions _options;

        public CloudFoundryCorsHandler(IEndpointOptions options, IEnumerable<ISecurityService> securityServices, IEnumerable<IManagementOptions> mgmtOptions, ILogger<CloudFoundryCorsHandler> logger = null)
            : base(securityServices, mgmtOptions, new List<HttpMethod> { HttpMethod.Options }, false, logger)
        {
            _options = options;
            _mgmtOptions = mgmtOptions;
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public CloudFoundryCorsHandler(CloudFoundryOptions options, IEnumerable<ISecurityService> securityServices, ILogger<CloudFoundryCorsHandler> logger = null)
           : base(securityServices, new List<HttpMethod> { HttpMethod.Options }, false, logger)
        {
            _options = options;
        }

        public override bool RequestVerbAndPathMatch(string httpMethod, string requestPath)
        {
            _logger?.LogTrace("RequestVerbAndPathMatch {httpMethod}/{requestPath}/{optionsPath} request", httpMethod, requestPath, _options.Path);
            return PathMatches(requestPath) && _allowedMethods.Any(m => m.Method.Equals(httpMethod));
        }

        public override void HandleRequest(HttpContextBase context)
        {
            _logger?.LogTrace("Processing {SteeltoeEndpoint} request", typeof(CloudFoundryCorsHandler));
            if (context.Request.HttpMethod == "OPTIONS")
            {
                var reqMethods = context.Request.Headers.Get("Access-Control-Request-Method");
                context.Response.Headers.Set("Access-Control-Allow-Methods", reqMethods ?? "GET,PUT");

                context.Response.Headers.Set("Access-Control-Allow-Origin", context.Request.Headers.Get("Origin"));

                var reqHeaders = context.Request.Headers.Get("Access-Control-Request-Headers");
                context.Response.Headers.Set("Access-Control-Allow-Headers", reqHeaders ?? "Authorization");

                context.Response.StatusCode = (int)HttpStatusCode.NoContent;
            }
        }

        public override Task<bool> IsAccessAllowed(HttpContextBase context)
        {
            return Task.FromResult(true);
        }

        private bool PathMatches(string requestPath)
        {
            if (_mgmtOptions == null)
            {
                return requestPath.StartsWith(_options.Path);
            }
            else
            {
                foreach (var mgmt in _mgmtOptions)
                {
                    var path = mgmt.Path;
                    if (!path.EndsWith("/") && !string.IsNullOrEmpty(_options.Id))
                    {
                        path += "/";
                    }

                    var fullPath = path + _options.Id;
                    if (requestPath.StartsWith(fullPath))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
