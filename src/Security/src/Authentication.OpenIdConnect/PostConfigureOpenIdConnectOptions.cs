// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Steeltoe.Security.Authentication.OpenIdConnect;

internal sealed class PostConfigureOpenIdConnectOptions : IPostConfigureOptions<OpenIdConnectOptions>
{
    private readonly TokenKeyResolver _tokenKeyResolver;

    public PostConfigureOpenIdConnectOptions(TokenKeyResolver tokenKeyResolver)
    {
        ArgumentNullException.ThrowIfNull(tokenKeyResolver);

        _tokenKeyResolver = tokenKeyResolver;
    }

    private static Task MapScopesToClaimsAsync(TokenValidatedContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // The ClaimsIdentity is built off the id_token, but scopes are returned in the access_token.
        // Identify scopes not already present as claims and add them to the ClaimsIdentity.

        if (context.Principal?.Identity is not ClaimsIdentity claimsIdentity)
        {
            return Task.CompletedTask;
        }

        string? scopes = context.TokenEndpointResponse?.Scope;

        if (scopes == null)
        {
            return Task.CompletedTask;
        }

        IEnumerable<Claim> claimsFromScopes = scopes.Split(' ')
            .Where(scope => !claimsIdentity.Claims.Any(claim => claim.Type == "scope" && claim.Value == scope))
            .Select(claimValue => new Claim("scope", claimValue));

        claimsIdentity.AddClaims(claimsFromScopes);

        return Task.CompletedTask;
    }

    public void PostConfigure(string? name, OpenIdConnectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Events.OnTokenValidated = MapScopesToClaimsAsync;

        options.ResponseType = OpenIdConnectResponseType.Code;
        options.SignInScheme ??= CookieAuthenticationDefaults.AuthenticationScheme;
        options.TokenValidationParameters.NameClaimType = "user_name";

        if (options.Authority == null)
        {
            return;
        }

        options.TokenValidationParameters.ValidIssuer = $"{options.Authority}/oauth/token";

        options.TokenValidationParameters.IssuerSigningKeyResolver = (_, _, keyId, _) =>
        {
            JsonWebKey? key = _tokenKeyResolver.ResolveSigningKey(options.Authority, keyId, options.Backchannel);
            return key != null ? [key] : [];
        };
    }
}
