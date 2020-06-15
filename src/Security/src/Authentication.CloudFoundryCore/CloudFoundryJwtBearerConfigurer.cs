// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Steeltoe.Connector.Services;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public static class CloudFoundryJwtBearerConfigurer
    {
        internal static void Configure(SsoServiceInfo si, JwtBearerOptions jwtOptions, CloudFoundryJwtBearerOptions options)
        {
            if (jwtOptions == null || options == null)
            {
                return;
            }

            if (si != null)
            {
                options.JwtKeyUrl = si.AuthDomain + CloudFoundryDefaults.JwtTokenUri;
            }

            jwtOptions.ClaimsIssuer = options.ClaimsIssuer;
            jwtOptions.BackchannelHttpHandler = CloudFoundryHelper.GetBackChannelHandler(options.ValidateCertificates);
            jwtOptions.TokenValidationParameters = CloudFoundryHelper.GetTokenValidationParameters(options.TokenValidationParameters, options.JwtKeyUrl, jwtOptions.BackchannelHttpHandler, options.ValidateCertificates);
            jwtOptions.SaveToken = options.SaveToken;
        }
    }
}
