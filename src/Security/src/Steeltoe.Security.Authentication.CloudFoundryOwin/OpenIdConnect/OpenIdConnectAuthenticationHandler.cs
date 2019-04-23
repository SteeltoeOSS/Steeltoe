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

using Microsoft.Extensions.Logging;
using Microsoft.Owin;
using Microsoft.Owin.Infrastructure;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Steeltoe.Security.Authentication.CloudFoundry.Owin
{
    public class OpenIdConnectAuthenticationHandler : AuthenticationHandler<OpenIdConnectOptions>
    {
        private readonly ILogger _logger;

        public OpenIdConnectAuthenticationHandler(ILogger logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Invoked for every request.As this is passive middleware, we only want to branch off
        /// the authentication flow if the request being invoked is the callback path we gave as a redirect
        /// uri to the auth flow when invoking the IDP
        /// </summary>
        /// <returns>A bool used to determine whether or not to continue down the OWIN pipline</returns>
        public override async Task<bool> InvokeAsync()
        {
            if (Options.CallbackPath.HasValue && Options.CallbackPath == Request.Path)
            {
                var ticket = await AuthenticateAsync();
                if (ticket != null)
                {
                    Context.Authentication.SignIn(ticket.Properties, ticket.Identity);
                    Response.Redirect(ticket.Properties.RedirectUri);

                    // Short-circuit, stopping rest of owin pipeline.
                    return true;
                }
            }

            // Nothing to see here, please disperse.
            return false;
        }

        protected override async Task<AuthenticationTicket> AuthenticateCoreAsync()
        {
            if (Options.CallbackPath.HasValue && Options.CallbackPath != (Request.PathBase + Request.Path))
            {
                return null;
            }

            var code = Request.Query["code"];
            if (code == null)
            {
                var error = Request.Query["error"];
                var error_description = Request.Query["error_description"];
                _logger?.LogError("No auth code detected in SSO response. Error: {error}, Description: {error_description}", error, error_description);
            }

            _logger?.LogDebug("Received an authorization code from IDP: " + code);
            _logger?.LogInformation("== exchanging auth code for token ==");

            var exchanger = new TokenExchanger(Options.AsAuthServerOptions(HostInfoFromRequest(Request) + Options.CallbackPath), null, _logger);
            var identity = await exchanger.ExchangeAuthCodeForClaimsIdentity(code);

            var properties = Options.StateDataFormat.Unprotect(Request.Query["state"]);

            return new AuthenticationTicket(identity, properties);
        }

        protected override Task InitializeCoreAsync()
        {
            return base.InitializeCoreAsync();
        }

        protected override Task ApplyResponseChallengeAsync()
        {
            if (Response.StatusCode == 401)
            {
                var challenge = Helper.LookupChallenge(Options.AuthenticationType, Options.AuthenticationMode);

                // If there is an authorization challenge for this request, then we know we haven't completed an IDP
                // pass yet. Redirect to the IDP in the hopes that we get a code passed back to our callback URL.
                if (challenge != null)
                {
                    var state = challenge.Properties;

                    if (string.IsNullOrEmpty(state.RedirectUri))
                    {
                        state.RedirectUri = Request.Uri.ToString();
                    }

                    var idpRedirectUri = UriUtility.CalculateFullRedirectUri(Options, Request);
                    _logger?.LogInformation("Redirecting to Identity Provider at {idpRedirectUri}", idpRedirectUri);

                    var stateString = Options.StateDataFormat.Protect(state);
                    Response.Redirect(WebUtilities.AddQueryString(idpRedirectUri, "state", stateString));
                }
            }

            return Task.FromResult<object>(null);
        }

        private string HostInfoFromRequest(IOwinRequest request)
        {
            return request.Scheme + Uri.SchemeDelimiter + request.Host;
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    [Obsolete("This class has been renamed OpenIdConnectAuthenticationHandler")]
    public class OpenIDConnectAuthenticationHandler : OpenIdConnectAuthenticationHandler
#pragma warning restore SA1402 // File may only contain a single class
    {
    }
}
