// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Steeltoe.Security.Authentication.CloudFoundry;

public class CloudFoundryOpenIdConnectOptions : OpenIdConnectOptions
{
    //// https://leastprivilege.com/2017/11/15/missing-claims-in-the-asp-net-core-2-openid-connect-handler/

    public CloudFoundryOpenIdConnectOptions()
    {
        AuthenticationMethod = OpenIdConnectRedirectBehavior.RedirectGet;
        Authority = $"https://{CloudFoundryDefaults.OAuthServiceUrl}";
        CallbackPath = new PathString(CloudFoundryDefaults.CallbackPath);
        ClaimsIssuer = CloudFoundryDefaults.AuthenticationScheme;
        ClientId = CloudFoundryDefaults.ClientId;
        ClientSecret = CloudFoundryDefaults.ClientSecret;
        ResponseType = OpenIdConnectResponseType.Code;
        SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

        // http://irisclasson.com/2018/09/18/asp-net-core-openidconnect-why-is-the-claimsprincipal-name-null/
        TokenValidationParameters.NameClaimType = "user_name";
        TokenValidationParameters.ValidateAudience = true;
        TokenValidationParameters.ValidateIssuer = true;
        TokenValidationParameters.ValidateLifetime = true;
    }

    /// <summary>
    /// Gets or sets additional scopes beyond openid and profile when requesting tokens
    /// </summary>
    public string AdditionalScopes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to validate auth server certificate
    /// </summary>
    public bool ValidateCertificates { get; set; } = true;

    /// <summary>
    /// Gets or sets the timeout (in ms) for calls to the auth server
    /// </summary>
    public int Timeout { get; set; } = 100000;

    internal AuthServerOptions BaseOptions(string updatedClientId)
    {
        return new AuthServerOptions
        {
            ClientId = updatedClientId ?? ClientId,
            ClientSecret = ClientSecret,
            ValidateCertificates = ValidateCertificates,
            AuthorizationUrl = Authority,
            ClientTimeout = Timeout
        };
    }
}
