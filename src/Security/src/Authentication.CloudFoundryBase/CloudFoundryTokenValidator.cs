// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Steeltoe.Security.Authentication.CloudFoundry;

public class CloudFoundryTokenValidator
{
    private readonly AuthServerOptions _options;

    public CloudFoundryTokenValidator(AuthServerOptions options = null)
    {
        _options = options ?? new AuthServerOptions();
    }

    /// <summary>
    /// Validate that a token was issued by UAA.
    /// </summary>
    /// <param name="issuer">
    /// Token issuer.
    /// </param>
    /// <param name="securityToken">
    /// [Not used] Token to validate.
    /// </param>
    /// <param name="validationParameters">
    /// [Not used].
    /// </param>
    /// <returns>
    /// The issuer, if valid, else <see langword="null" />.
    /// </returns>
    public virtual string ValidateIssuer(string issuer, SecurityToken securityToken, TokenValidationParameters validationParameters)
    {
        if (issuer.Contains("uaa"))
        {
            return issuer;
        }

        return null;
    }

    /// <summary>
    /// Validate that a token was meant for approved audience(s).
    /// </summary>
    /// <param name="audiences">
    /// The list of audiences the token is valid for.
    /// </param>
    /// <param name="securityToken">
    /// [Not used] The token being validated.
    /// </param>
    /// <param name="validationParameters">
    /// [Not used].
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the audience matches the client id or any value in AdditionalAudiences.
    /// </returns>
    public virtual bool ValidateAudience(IEnumerable<string> audiences, SecurityToken securityToken, TokenValidationParameters validationParameters)
    {
        foreach (string audience in audiences)
        {
            if (audience.Equals(_options.ClientId))
            {
                return true;
            }

            if (_options.AdditionalAudiences != null)
            {
                bool found = _options.AdditionalAudiences.Any(x => x.Equals(audience));

                if (found)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// This method validates scopes provided in configuration, to perform scope based Authorization.
    /// </summary>
    /// <param name="validJwt">
    /// JSON Web token.
    /// </param>
    /// <returns>
    /// true if scopes validated.
    /// </returns>
    protected virtual bool ValidateScopes(JwtSecurityToken validJwt)
    {
        if (_options.RequiredScopes == null || !_options.RequiredScopes.Any())
        {
            return true; // no check
        }

        if (!validJwt.Claims.Any(x => x.Type.Equals("scope") || x.Type.Equals("authorities")))
        {
            return false; // no scopes at all
        }

        foreach (Claim claim in validJwt.Claims)
        {
            if (claim.Type.Equals("scope") || (claim.Type.Equals("authorities") && _options.RequiredScopes.Any(x => x.Equals(claim.Value))))
            {
                return true;
            }
        }

        return false;
    }
}
