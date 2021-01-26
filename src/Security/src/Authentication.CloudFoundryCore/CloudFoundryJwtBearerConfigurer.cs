// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Steeltoe.CloudFoundry.Connector.Services;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public static class CloudFoundryJwtBearerConfigurer
    {
        internal static void Configure(
            SsoServiceInfo si,
            JwtBearerOptions jwtOptions,
            CloudFoundryJwtBearerOptions options,
            OAuthServiceInfo oAuthServiceInfo = null)
        {
            if (jwtOptions is null || options is null)
            {
                return;
            }

            if (si is not null)
            {
                options.JwtKeyUrl = si.AuthDomain + CloudFoundryDefaults.JwtTokenUri;
            }
            else if (oAuthServiceInfo is not null)
            {
                options.JwtKeyUrl = oAuthServiceInfo.AuthDomain + CloudFoundryDefaults.JwtTokenUri;
            }

            jwtOptions.ClaimsIssuer = options.ClaimsIssuer;
            jwtOptions.BackchannelHttpHandler = CloudFoundryHelper.GetBackChannelHandler(options.ValidateCertificates);
            jwtOptions.TokenValidationParameters = CloudFoundryHelper.GetTokenValidationParameters(options.TokenValidationParameters, options.JwtKeyUrl, jwtOptions.BackchannelHttpHandler, options.ValidateCertificates);
            jwtOptions.SaveToken = options.SaveToken;
        }
    }
}
