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
                _logger?.LogTrace("Request path matches auth callback path");
                var ticket = await AuthenticateAsync().ConfigureAwait(false);
                if (ticket != null)
                {
                    _logger?.LogTrace("Authentication ticket found");
                    Context.Authentication.SignIn(ticket.Properties, ticket.Identity);
                    Response.Redirect(ticket.Properties.RedirectUri);

                    // Short-circuit, stopping rest of owin pipeline.
                    return true;
                }

                _logger?.LogDebug("Request path matched callback path, but no auth ticket was not found");
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
            var identity = await exchanger.ExchangeAuthCodeForClaimsIdentity(code).ConfigureAwait(false);

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
                _logger?.LogTrace("Status code of 401 encountered, checking for auth challenge");
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

                _logger?.LogTrace("No auth challenge found for this 401 response");
            }

            return Task.FromResult<object>(null);
        }

        protected override Task ApplyResponseCoreAsync()
        {
            _logger?.LogTrace("Entering ApplyResponseCoreAsync with OpenIdConnect");
            return base.ApplyResponseCoreAsync();
        }

        protected override Task ApplyResponseGrantAsync()
        {
            _logger?.LogTrace("Entering ApplyResponseGrantAsync with OpenIdConnect");
            return base.ApplyResponseGrantAsync();
        }

        protected override Task TeardownCoreAsync()
        {
            _logger?.LogTrace("Entering TeardownCoreAsync with OpenIdConnect");
            return base.TeardownCoreAsync();
        }

        private string HostInfoFromRequest(IOwinRequest request)
        {
            return request.Scheme + Uri.SchemeDelimiter + request.Host;
        }
    }
}
