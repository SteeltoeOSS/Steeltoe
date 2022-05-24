// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Steeltoe.Connector.Services;
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

            // remove profile scope
            oidcOptions.Scope.Clear();
            oidcOptions.Scope.Add("openid");

            // add other scopes
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

            oidcOptions.TokenValidationParameters = CloudFoundryHelper.GetTokenValidationParameters(
                cfOptions.TokenValidationParameters,
                oidcOptions.Authority + CloudFoundryDefaults.JwtTokenUri,
                oidcOptions.BackchannelHttpHandler,
                cfOptions.ValidateCertificates,
                cfOptions.BaseOptions(oidcOptions.ClientId));

            // the ClaimsIdentity is built off the id_token, but scopes are returned in the access_token. Copy them as claims
            oidcOptions.Events.OnTokenValidated = MapScopesToClaims;
        }

        internal static readonly Func<TokenValidatedContext, Task> MapScopesToClaims = context =>
        {
            // get claimsId
            var claimsId = context.Principal.Identity as ClaimsIdentity;

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
