// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Security.Authentication.Shared;

namespace Steeltoe.Security.Authentication.JwtBearer;

internal sealed class PostConfigureJwtBearerOptions(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    : IPostConfigureOptions<JwtBearerOptions>
{
    private const string BearerConfigurationKeyPrefix = "Authentication:Schemes:Bearer";

    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        string? clientId = configuration.GetValue<string>($"{BearerConfigurationKeyPrefix}:ClientId");

        if (!string.IsNullOrEmpty(clientId) && options.TokenValidationParameters.ValidAudiences?.Contains(clientId) != true)
        {
            var audiences = new List<string>(options.TokenValidationParameters.ValidAudiences ?? [])
            {
                clientId
            };

            options.TokenValidationParameters.ValidAudiences = audiences;
        }

        if (options.Authority?.Equals(SteeltoeSecurityDefaults.LocalUAAPath, StringComparison.InvariantCultureIgnoreCase) == true)
        {
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters.ValidIssuer = $"{SteeltoeSecurityDefaults.LocalUAAPath}/uaa/oauth/token";
        }
        else if (options.Authority != null)
        {
            options.TokenValidationParameters.ValidIssuer = $"{options.Authority}/oauth/token";
        }

        if (options.Authority != null)
        {
            // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
            var keyResolver = new TokenKeyResolver(options.Authority, options.Backchannel ?? httpClientFactory.CreateClient("SteeltoeSecurity"));
            options.TokenValidationParameters.IssuerSigningKeyResolver = keyResolver.ResolveSigningKey;
        }
    }
}
