//
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
//

using Microsoft.Owin.Infrastructure;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Steeltoe.Security.Authentication.CloudFoundryOwin
{
    class OpenIDConnectAuthenticationHandler : AuthenticationHandler<OpenIDConnectOptions>
    {
        protected override async Task<AuthenticationTicket> AuthenticateCoreAsync()
        {

            var code = Request.Query["code"];
            Debug.WriteLine("Received an authorization code from IDP: " + code);
            Debug.WriteLine("== exchanging for token ==");
            // ASP.Net Identity requires the NameIdentitifer field to be set or it won't  
            // accept the external login (AuthenticationManagerExtensions.GetExternalLoginInfo)
            var identity = await TokenExchanger.ExchangeCodeForToken(code, Options);

            var properties = Options.StateDataFormat.Unprotect(Request.Query["state"]);

            //return Task.FromResult(new AuthenticationTicket(identity, properties));
            var ticket = new AuthenticationTicket(identity, properties);
            return ticket;
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

                    var stateString = Options.StateDataFormat.Protect(state);
                    Debug.WriteLine("Challenge redirecting to IDP...");

                    var idpRedirectUri = UriUtility.CalculateFullRedirectUri(Options);
                    Response.Redirect(WebUtilities.AddQueryString(idpRedirectUri, "state", stateString));
                }
            }

            return Task.FromResult<object>(null);
        }


        protected override Task InitializeCoreAsync()
        {
            return base.InitializeCoreAsync();
        }

        /*
         * Invoked for every request. As this is passive middleware, we only want to branch off
         * the authentication flow if the request being invoked is the callback path we gave as a redirect
         * uri to the auth flow when invoking the IDP
         */
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
    }
}
