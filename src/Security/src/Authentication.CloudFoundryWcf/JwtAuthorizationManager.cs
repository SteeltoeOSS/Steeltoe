// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
        private readonly CloudFoundryOptions _options;

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

            if (operationContext.RequestContext.RequestMessage.Properties.TryGetValue(HttpRequestMessageProperty.Name, out var httpRequestMessageObject))
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
                var cur = HttpContext.Current;
                if (cur != null)
                {
                    cur.User = principal;
                }
            }
        }
    }
}
