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

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Steeltoe.CloudFoundry.Connector.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    internal static class CloudFoundryOpenIdConnectConfigurer
    {
        /// <summary>
        /// Maps service info credentials and 'security:oauth2:client' info onto OpenIdConnectOptions
        /// </summary>
        /// <param name="si">Service info credentials parsed from VCAP_SERVICES</param>
        /// <param name="oidcOptions">OpenId Connect options to be configured</param>
        /// <param name="cfOptions">Cloud Foundry-related OpenId Connect configuration options</param>
        internal static void Configure(SsoServiceInfo si, OpenIdConnectOptions oidcOptions, CloudFoundryOpenIdConnectOptions cfOptions)
        {
            if (oidcOptions == null || cfOptions == null)
            {
                return;
            }

            if (si != null)
            {
                oidcOptions.Authority = si.AuthDomain;
                oidcOptions.ClientId = si.ClientId;
                oidcOptions.ClientSecret = si.ClientSecret;
            }
            else
            {
                oidcOptions.Authority = cfOptions.Authority;
                oidcOptions.ClientId = cfOptions.ClientId;
                oidcOptions.ClientSecret = cfOptions.ClientSecret;
            }

            oidcOptions.AuthenticationMethod = cfOptions.AuthenticationMethod;
            oidcOptions.BackchannelHttpHandler = CloudFoundryHelper.GetBackChannelHandler(cfOptions.ValidateCertificates);
            oidcOptions.CallbackPath = cfOptions.CallbackPath;
            oidcOptions.ClaimsIssuer = cfOptions.ClaimsIssuer;
            oidcOptions.ResponseType = cfOptions.ResponseType;
            oidcOptions.SaveTokens = cfOptions.SaveTokens;
            oidcOptions.SignInScheme = cfOptions.SignInScheme;
            if (!string.IsNullOrEmpty(cfOptions.AdditionalScopes))
            {
                foreach (var s in cfOptions.AdditionalScopes.Split(' '))
                {
                    if (!oidcOptions.Scope.Contains(s))
                    {
                        oidcOptions.Scope.Add(s);
                    }
                }
            }

            // http://irisclasson.com/2018/09/18/asp-net-core-openidconnect-why-is-the-claimsprincipal-name-null/
            oidcOptions.TokenValidationParameters.NameClaimType = cfOptions.TokenValidationParameters.NameClaimType;

            // main objective here is to set the IssuerSigningKeyResolver to work around an issue parsing the N value of the signing key in FullFramework
            oidcOptions.TokenValidationParameters = CloudFoundryHelper.GetTokenValidationParameters(oidcOptions.TokenValidationParameters, oidcOptions.Authority + CloudFoundryDefaults.JwtTokenUri, oidcOptions.BackchannelHttpHandler, cfOptions.ValidateCertificates, cfOptions.BaseOptions(oidcOptions.ClientId));

            oidcOptions.TokenValidationParameters.ValidateAudience = cfOptions.TokenValidationParameters.ValidateAudience;
            oidcOptions.TokenValidationParameters.ValidateLifetime = cfOptions.TokenValidationParameters.ValidateLifetime;

            // the ClaimsIdentity is built off the id_token, but scopes are returned in the access_token. Copy them as claims
            oidcOptions.Events.OnTokenValidated = MapScopesToClaims;
        }

        internal static Func<TokenValidatedContext, Task> MapScopesToClaims = (context) =>
        {
            // get claimsId
            ClaimsIdentity claimsId = context.Principal.Identity as ClaimsIdentity;

            // get scopes
            var scopes = context.TokenEndpointResponse.Scope;

            // make sure id has scopes
            foreach (var scope in scopes.Split(' '))
            {
                if (!claimsId.Claims.Any(c => c.Type == "scope" && c.Value.Equals(scope)))
                {
                    claimsId.AddClaim(new Claim("scope", scope));
                }
            }

            return Task.FromResult(0);
        };
    }
}
