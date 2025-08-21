// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Security.Authentication.JwtBearer;

internal sealed class PostConfigureJwtBearerOptions : IPostConfigureOptions<JwtBearerOptions>
{
    private const string BearerConfigurationKeyPrefix = "Authentication:Schemes:Bearer";
    private readonly IConfiguration _configuration;

    public PostConfigureJwtBearerOptions(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _configuration = configuration;
    }

    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Set secure defaults for token validation
        options.TokenValidationParameters.ValidateIssuer = true;
        options.TokenValidationParameters.ValidateAudience = true;
        options.TokenValidationParameters.ValidateLifetime = true;
        options.TokenValidationParameters.ValidateIssuerSigningKey = true;
        options.TokenValidationParameters.RequireExpirationTime = true;
        options.TokenValidationParameters.RequireSignedTokens = true;
        
        // Set clock skew to a reasonable value (default is 5 minutes, we reduce to 30 seconds)
        options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(30);

        string? clientId = _configuration.GetValue<string>($"{BearerConfigurationKeyPrefix}:ClientId");

        if (!string.IsNullOrEmpty(clientId) && options.TokenValidationParameters.ValidAudiences?.Contains(clientId) != true)
        {
            string[] audiences =
            [
                ..options.TokenValidationParameters.ValidAudiences ?? [],
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

        var keyResolver = new TokenKeyResolver(options.Authority, options.Backchannel);
        options.TokenValidationParameters.IssuerSigningKeyResolver = (_, _, keyId, _) => keyResolver.ResolveSigningKey(keyId);
    }
}
