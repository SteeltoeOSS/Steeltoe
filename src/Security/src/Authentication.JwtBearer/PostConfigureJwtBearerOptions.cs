// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

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

        options.TokenValidationParameters.ValidIssuer = $"{options.Authority}/oauth/token";

        var keyResolver = new TokenKeyResolver(options.Authority, options.Backchannel);
        options.TokenValidationParameters.IssuerSigningKeyResolver = (_, _, keyId, _) => keyResolver.ResolveSigningKey(keyId);
    }
}
