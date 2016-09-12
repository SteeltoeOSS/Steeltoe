//
// Copyright 2015 the original author or authors.
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
//

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using SteelToe.CloudFoundry.Connector.OAuth;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace SteelToe.Security.Authentication.CloudFoundry
{

    public static class CloudFoundryAppBuilderExtensions
    {
        public static IApplicationBuilder UseCloudFoundryAuthentication(this IApplicationBuilder builder )
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            CloudFoundryOptions options = UpdateCloudFoundryOptions(builder, new CloudFoundryOptions());
            options.TokenValidationParameters = GetTokenValidationParameters(options);

            var cookieOptions = GetCookieOptions(options);
            builder.UseCookieAuthentication(cookieOptions);

            return builder.UseMiddleware<CloudFoundryMiddleware>(Options.Create(options));
        }

        public static IApplicationBuilder UseCloudFoundryAuthentication(this IApplicationBuilder builder, CloudFoundryOptions options)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options = UpdateCloudFoundryOptions(builder, options);
            options.TokenValidationParameters = GetTokenValidationParameters(options);

            var cookieOptions = GetCookieOptions(options);
            builder.UseCookieAuthentication(cookieOptions);

            return builder.UseMiddleware<CloudFoundryMiddleware>(Options.Create(options));
        }
        public static IApplicationBuilder UseCloudFoundryJwtAuthentication(this IApplicationBuilder builder, CloudFoundryOptions options)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options = UpdateCloudFoundryOptions(builder, options);

            var bearerOpts = GetJwtBearerOptions(options);
            return builder.UseJwtBearerAuthentication(bearerOpts);

        }

        public static IApplicationBuilder UseCloudFoundryJwtAuthentication(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var options = UpdateCloudFoundryOptions(builder, new CloudFoundryOptions());

            var bearerOpts = GetJwtBearerOptions(options);
            return builder.UseJwtBearerAuthentication(bearerOpts);

        }

        private static JwtBearerOptions GetJwtBearerOptions(CloudFoundryOptions options)
        {
            if (options.JwtBearerOptions != null)
            {
                return options.JwtBearerOptions;
            }

            return new JwtBearerOptions()
            {
                TokenValidationParameters = GetTokenValidationParameters(options)
                
            };
        }

        private static TokenValidationParameters GetTokenValidationParameters(CloudFoundryOptions options)
        {
            if (options.TokenValidationParameters != null)
            {
                return options.TokenValidationParameters;
            }

            var parameters = new TokenValidationParameters();
            options.TokenKeyResolver = new CloudFoundryTokenKeyResolver(options);
            options.TokenValidator = new CloudFoundryTokenValidator(options);

            parameters.ValidateAudience = true;
            parameters.ValidateIssuer = true;
            parameters.ValidateLifetime = true;
   
            
            parameters.IssuerSigningKeyResolver = options.TokenKeyResolver.ResolveSigningKey;
            parameters.IssuerValidator = options.TokenValidator.ValidateIssuer;
            parameters.AudienceValidator = options.TokenValidator.ValidateAudience;

            return parameters;
        }

        private static CookieAuthenticationOptions GetCookieOptions(CloudFoundryOptions options)
        {
            var cookieOptions = new CookieAuthenticationOptions()
            {
                AuthenticationScheme = CloudFoundryOptions.AUTHENTICATION_SCHEME,
                AutomaticAuthenticate = true,
                AutomaticChallenge = false,
                CookieName = CloudFoundryOptions.AUTHENTICATION_SCHEME
            
            };


            if (options.AccessDeniedPath != null)
            {
                cookieOptions.AccessDeniedPath = options.AccessDeniedPath;
                cookieOptions.LogoutPath = options.AccessDeniedPath;
            }

            if (options.TokenValidator != null)
            {
                cookieOptions.Events = new CookieAuthenticationEvents()
                {
                    OnValidatePrincipal = options.TokenValidator.ValidateCookieAsync
                };
            }

            return cookieOptions;

        }

        private static CloudFoundryOptions UpdateCloudFoundryOptions(IApplicationBuilder builder, CloudFoundryOptions cloudOpts)
        {
            var iopts = builder.ApplicationServices.GetService(typeof(IOptions<OAuthServiceOptions>)) as IOptions<OAuthServiceOptions>;
            var signonOpts = iopts?.Value;
            cloudOpts.UpdateOptions(signonOpts);
            cloudOpts.BackchannelHttpHandler = cloudOpts.GetBackChannelHandler();
           
            return cloudOpts;

        }

    }
}
