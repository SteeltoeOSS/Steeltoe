// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Steeltoe.Connector.Services;
using System.Security.Claims;

namespace Steeltoe.Security.Authentication.CloudFoundry;

internal static class CloudFoundryOpenIdConnectConfigurer
{
    /// <summary>
    /// Maps service info credentials and 'security:oauth2:client' info onto OpenIdConnectOptions.
    /// </summary>
    /// <param name="si">Service info credentials parsed from VCAP_SERVICES.</param>
    /// <param name="openIdOptions">OpenId Connect options to be configured.</param>
    /// <param name="cloudFoundryOptions">Cloud Foundry-related OpenId Connect configuration options.</param>
    internal static void Configure(SsoServiceInfo si, OpenIdConnectOptions openIdOptions, CloudFoundryOpenIdConnectOptions cloudFoundryOptions)
    {
        if (openIdOptions == null || cloudFoundryOptions == null)
        {
            return;
        }

        if (si != null)
        {
            openIdOptions.Authority = si.AuthDomain;
            openIdOptions.ClientId = si.ClientId;
            openIdOptions.ClientSecret = si.ClientSecret;
        }
        else
        {
            openIdOptions.Authority = cloudFoundryOptions.Authority;
            openIdOptions.ClientId = cloudFoundryOptions.ClientId;
            openIdOptions.ClientSecret = cloudFoundryOptions.ClientSecret;
        }

        openIdOptions.AuthenticationMethod = cloudFoundryOptions.AuthenticationMethod;
        openIdOptions.BackchannelHttpHandler = CloudFoundryHelper.GetBackChannelHandler(cloudFoundryOptions.ValidateCertificates);
        openIdOptions.CallbackPath = cloudFoundryOptions.CallbackPath;
        openIdOptions.ClaimsIssuer = cloudFoundryOptions.ClaimsIssuer;
        openIdOptions.ResponseType = cloudFoundryOptions.ResponseType;
        openIdOptions.SaveTokens = cloudFoundryOptions.SaveTokens;
        openIdOptions.SignInScheme = cloudFoundryOptions.SignInScheme;

        // remove profile scope
        openIdOptions.Scope.Clear();
        openIdOptions.Scope.Add("openid");

        // add other scopes
        if (!string.IsNullOrEmpty(cloudFoundryOptions.AdditionalScopes))
        {
            foreach (var s in cloudFoundryOptions.AdditionalScopes.Split(' '))
            {
                if (!openIdOptions.Scope.Contains(s))
                {
                    openIdOptions.Scope.Add(s);
                }
            }
        }

        openIdOptions.TokenValidationParameters = CloudFoundryHelper.GetTokenValidationParameters(
            cloudFoundryOptions.TokenValidationParameters,
            openIdOptions.Authority + CloudFoundryDefaults.JwtTokenUri,
            openIdOptions.BackchannelHttpHandler,
            cloudFoundryOptions.ValidateCertificates,
            cloudFoundryOptions.BaseOptions(openIdOptions.ClientId));

        // the ClaimsIdentity is built off the id_token, but scopes are returned in the access_token. Copy them as claims
        openIdOptions.Events.OnTokenValidated = MapScopesToClaims;
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
