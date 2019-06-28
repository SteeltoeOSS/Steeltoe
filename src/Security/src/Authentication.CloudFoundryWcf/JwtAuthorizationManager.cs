// Copyright 2017 the original author or authors.
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

using System;
using System.Net;
using System.Security.Claims;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Web;
using System.Web.Hosting;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf
{
    // https://docs.microsoft.com/en-us/dotnet/framework/wcf/extending/custom-authorization
    public class JwtAuthorizationManager : ServiceAuthorizationManager
    {
        private static CloudFoundryOptions _options;

        public JwtAuthorizationManager()
            : base()
        {
        }

        public JwtAuthorizationManager(CloudFoundryOptions options)
            : base()
        {
            _options = options;
        }

        internal ClaimsPrincipal GetPrincipalFromRequestHeaders(WebHeaderCollection headers)
        {
            // Fail if SSO Config is missing
            if (_options?.AuthorizationUrl == null || _options?.AuthorizationUrl?.Length == 0)
            {
                CloudFoundryWcfTokenValidator.ThrowJwtException("SSO Configuration is missing", null);
            }

            // check if any auth header is present
            if (string.IsNullOrEmpty(headers["Authorization"]))
            {
                CloudFoundryWcfTokenValidator.ThrowJwtException("No Authorization header", null);
            }

            // check if the auth header has a bearer token format
            if (!headers["Authorization"].StartsWith("Bearer", StringComparison.InvariantCultureIgnoreCase))
            {
                CloudFoundryWcfTokenValidator.ThrowJwtException("Wrong Token Format", null);
            }

            // get just the token out of the header value
            string jwt;
            try
            {
                jwt = headers["Authorization"].Split(' ')[1];

                // Return an identity from validated token
                return _options.TokenValidator.ValidateToken(jwt);
            }
            catch (IndexOutOfRangeException)
            {
                CloudFoundryWcfTokenValidator.ThrowJwtException("No Token", null);
            }

            throw new NotImplementedException("Unable to locate a Principal in the request header");
        }

        protected override bool CheckAccessCore(OperationContext operationContext)
        {
            HttpRequestMessageProperty httpRequestMessage;

            if (operationContext.RequestContext.RequestMessage.Properties.TryGetValue(HttpRequestMessageProperty.Name, out object httpRequestMessageObject))
            {
                httpRequestMessage = httpRequestMessageObject as HttpRequestMessageProperty;
                var claimsPrincipal = GetPrincipalFromRequestHeaders(httpRequestMessage.Headers);
                if (claimsPrincipal != null)
                {
                    // Set the Principal created from token
                    SetPrincipal(operationContext, claimsPrincipal);
                    return true;
                }
            }

            return false;
        }

        protected ClaimsPrincipal GetPrincipal(OperationContext operationContext)
        {
            var properties = operationContext.ServiceSecurityContext.AuthorizationContext.Properties;
            return properties["Principal"] as ClaimsPrincipal;
        }

        private void SetPrincipal(OperationContext operationContext, ClaimsPrincipal principal)
        {
            var properties = operationContext.ServiceSecurityContext.AuthorizationContext.Properties;

            if (!properties.ContainsKey("Principal"))
            {
                properties.Add("Principal", principal);
            }
            else
            {
                properties["Principal"] = principal;
            }

            if (HostingEnvironment.IsHosted)
            {
                HttpContext cur = HttpContext.Current;
                if (cur != null)
                {
                    cur.User = principal;
                }
            }
        }
    }
}
