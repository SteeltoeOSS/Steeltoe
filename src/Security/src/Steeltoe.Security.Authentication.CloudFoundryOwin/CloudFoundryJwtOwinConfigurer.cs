// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Security.Jwt;
using Steeltoe.CloudFoundry.Connector.Services;
using System.Net.Http;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public static class CloudFoundryJwtOwinConfigurer
    {
        internal static void Configure(SsoServiceInfo si, JwtBearerAuthenticationOptions jwtOptions, CloudFoundryJwtBearerAuthenticationOptions options)
        {
            if (jwtOptions == null || options == null)
            {
                return;
            }

            if (si != null)
            {
                options.JwtKeyUrl = si.AuthDomain + CloudFoundryDefaults.JwtTokenKey;
            }

            // jwtOptions.ClaimsIssuer = options.ClaimsIssuer;
            // jwtOptions.BackchannelHttpHandler = CloudFoundryHelper.GetBackChannelHandler(options.ValidateCertificates);
            // jwtOptions.TokenValidationParameters = GetTokenValidationParameters(jwtOptions.TokenValidationParameters, options.JwtKeyUrl, jwtOptions.BackchannelHttpHandler, options.ValidateCertificates);
            // jwtOptions.SaveToken = options.SaveToken;
        }

        internal static TokenValidationParameters GetTokenValidationParameters(TokenValidationParameters parameters, string keyUrl, HttpMessageHandler handler, bool validateCertificates)
        {
            if (parameters == null)
            {
                parameters = new TokenValidationParameters();
            }

            parameters.ValidateAudience = false;
            parameters.ValidateIssuer = true;
            parameters.ValidateLifetime = true;
            parameters.IssuerValidator = CloudFoundryTokenValidator.ValidateIssuer;

            var tkr = new CloudFoundryTokenKeyResolver(keyUrl, handler, validateCertificates);
            parameters.IssuerSigningKeyResolver = tkr.ResolveSigningKey;

            return parameters;
        }
    }
}
