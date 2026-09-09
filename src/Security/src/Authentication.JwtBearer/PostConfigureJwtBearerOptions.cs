// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Steeltoe.Common;

namespace Steeltoe.Security.Authentication.JwtBearer;

internal sealed class PostConfigureJwtBearerOptions : IPostConfigureOptions<JwtBearerOptions>
{
    private const string BearerConfigurationKeyPrefix = "Authentication:Schemes:Bearer";
    private readonly IConfiguration _configuration;
    private readonly TokenKeyResolver _tokenKeyResolver;

    public PostConfigureJwtBearerOptions(IConfiguration configuration, TokenKeyResolver tokenKeyResolver)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(tokenKeyResolver);

        _configuration = configuration;
        _tokenKeyResolver = tokenKeyResolver;
    }

    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        string? clientId = _configuration.GetValue<string>($"{BearerConfigurationKeyPrefix}:ClientId");

        if (!string.IsNullOrEmpty(clientId) && options.TokenValidationParameters.ValidAudiences?.Contains(clientId) != true)
        {
            string[] audiences =
            [
                .. options.TokenValidationParameters.ValidAudiences ?? [],
                clientId
            ];

            options.TokenValidationParameters.ValidAudiences = audiences;
        }

        if (options.Authority == null)
        {
            return;
        }

        if (Platform.IsCloudFoundry && options.Authority.Contains(".login", StringComparison.OrdinalIgnoreCase))
        {
            options.TokenValidationParameters.ValidIssuers =
            [
                $"{options.Authority}/oauth/token",
                $"{options.Authority.Replace(".login", ".uaa", StringComparison.OrdinalIgnoreCase)}/oauth/token"
            ];
        }
        else
        {
            options.TokenValidationParameters.ValidIssuer = $"{options.Authority}/oauth/token";
        }

        options.TokenValidationParameters.IssuerSigningKeyResolver = (_, _, keyId, _) =>
        {
            JsonWebKey? key = _tokenKeyResolver.ResolveSigningKey(options.Authority, keyId, options.Backchannel);
            return key != null ? [key] : [];
        };
    }
}
