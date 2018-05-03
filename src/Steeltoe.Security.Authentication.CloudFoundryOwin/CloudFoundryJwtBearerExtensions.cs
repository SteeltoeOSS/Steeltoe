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

using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Security.Jwt;
using Owin;
using Steeltoe.CloudFoundry.Connector;
using Steeltoe.CloudFoundry.Connector.Services;
using System.Net.Http;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public static class CloudFoundryJwtBearerExtensions
    {
        public static IAppBuilder UseCloudFoundryJwtBearerAuthentication(this IAppBuilder app, IConfiguration config)
        {
            var cloudFoundryOptions = new CloudFoundryJwtBearerAuthenticationOptions();
            var securitySection = config.GetSection(CloudFoundryDefaults.SECURITY_CLIENT_SECTION_PREFIX);
            securitySection.Bind(cloudFoundryOptions);

            SsoServiceInfo si = config.GetSingletonServiceInfo<SsoServiceInfo>();
            if (si == null)
            {
                return app;
            }

            var jwtTokenUrl = si.AuthDomain + CloudFoundryDefaults.JwtTokenKey;
            var httpMessageHandler = CloudFoundryHelper.GetBackChannelHandler(cloudFoundryOptions.ValidateCertificates);
            var tokenValidationParameters = GetTokenValidationParameters(jwtTokenUrl, httpMessageHandler, cloudFoundryOptions.ValidateCertificates);

            return app.UseJwtBearerAuthentication(
                new JwtBearerAuthenticationOptions
                {
                    TokenValidationParameters = tokenValidationParameters,
                });
        }

        internal static TokenValidationParameters GetTokenValidationParameters(string keyUrl, HttpMessageHandler handler, bool validateCertificates)
        {
            var parameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = true,
                ValidateLifetime = true,
                IssuerValidator = CloudFoundryTokenValidator.ValidateIssuer
            };

            var tkr = new CloudFoundryTokenKeyResolver(keyUrl, handler, validateCertificates);
            parameters.IssuerSigningKeyResolver = tkr.ResolveSigningKey;

            return parameters;
        }
    }
}
