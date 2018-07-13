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

        protected override bool CheckAccessCore(OperationContext operationContext)
        {
            HttpRequestMessageProperty httpRequestMessage;

            if (operationContext.RequestContext.RequestMessage.Properties.TryGetValue(HttpRequestMessageProperty.Name, out object httpRequestMessageObject))
            {
                httpRequestMessage = httpRequestMessageObject as HttpRequestMessageProperty;
                if (string.IsNullOrEmpty(httpRequestMessage.Headers["Authorization"]))
                {
                    CloudFoundryTokenValidator.ThrowJwtException("No Authorization header", null);
                }

                // Get Bearer token
                if (!httpRequestMessage.Headers["Authorization"].StartsWith("Bearer "))
                {
                    CloudFoundryTokenValidator.ThrowJwtException("No Token", null);
                }

                string jwt = httpRequestMessage.Headers["Authorization"].Split(' ')[1];
                if (string.IsNullOrEmpty(jwt))
                {
                    CloudFoundryTokenValidator.ThrowJwtException("Wrong Token Format", null);
                }

                // Get SSO Config
                if (_options?.OAuthServiceUrl == null || _options?.OAuthServiceUrl?.Length == 0)
                {
                    CloudFoundryTokenValidator.ThrowJwtException("SSO Configuration is missing", null);
                }

                // Validate Token
                ClaimsPrincipal claimsPrincipal = _options.TokenValidator.ValidateToken(jwt);
                if (claimsPrincipal == null)
                {
                    return false;
                }

                // Set the Principal created from token
                SetPrincipal(operationContext, claimsPrincipal);

                return true;
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
