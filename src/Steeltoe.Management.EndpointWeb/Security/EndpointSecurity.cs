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

using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.CloudFoundry;
using System;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;

namespace Steeltoe.Management.Endpoint.Security
{
    public class EndpointSecurity : ISecurityService
    {
        private ILogger<EndpointSecurity> _logger;
        private ICloudFoundryOptions _options;
        private SecurityHelper _helper;

        public EndpointSecurity(ICloudFoundryOptions options, ILogger<EndpointSecurity> logger = null)
        {
            _options = options;
            _logger = logger;
            _helper = new SecurityHelper(options, logger);
        }

        public async Task<bool> IsAccessAllowed(HttpContextBase context, IEndpointOptions target)
        {
           if (_helper.IsCloudFoundryRequest(context.Request.Path) && _options.IsEnabled)
            {
                _logger?.LogTrace("Beginning Endpoint Security Processing");

                var origin = context.Request.Headers.Get("Origin");
                context.Response.Headers.Set("Access-Control-Allow-Origin", origin ?? "*");

                if (target.IsSensitive && !HasSensitiveClaim(context))
                {
                    _logger?.LogTrace("Access denied! Target was marked sensitive, but did not have claim {0}", _options.Global.SensitiveClaim);
                    await _helper.ReturnError(context, new SecurityResult(HttpStatusCode.Unauthorized, _helper.ACCESS_DENIED_MESSAGE));
                    return false;
                }

                _logger?.LogTrace("Access granted!");
            }

            return true;
        }

        private bool HasSensitiveClaim(HttpContextBase context)
        {
            var claim = _options.Global.SensitiveClaim;
            var user = context.User;
            return claim != null &&
                    user != null &&
                    user.Identity.IsAuthenticated && ((ClaimsIdentity)user.Identity).HasClaim(claim.Type, claim.Value);
        }
    }
}
