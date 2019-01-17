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
using Microsoft.Extensions.Logging;
using Owin;
using Steeltoe.CloudFoundry.Connector;
using Steeltoe.CloudFoundry.Connector.Services;
using System;

namespace Steeltoe.Security.Authentication.CloudFoundry.Owin
{
    public static class CloudFoundryExtensions
    {
        /// <summary>
        /// Configures and adds <see cref="OpenIdConnectAuthenticationMiddleware" /> to the OWIN request pipeline />
        /// </summary>
        /// <param name="appBuilder">Your OWIN AppBuilder</param>
        /// <param name="configuration">Your application configuration</param>
        /// <param name="authenticationType">The identifier for this authentication type. MUST match the value used in your authentication challenge.</param>
        /// <param name="loggerFactory">For logging with SSO interactions</param>
        /// <returns>Your <see cref="IAppBuilder"/></returns>
        public static IAppBuilder UseCloudFoundryOpenIdConnect(this IAppBuilder appBuilder, IConfiguration configuration, string authenticationType = CloudFoundryDefaults.AuthenticationScheme, LoggerFactory loggerFactory = null)
        {
            if (appBuilder == null)
            {
                throw new ArgumentNullException(nameof(appBuilder));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            // get options with defaults
            var cloudFoundryOptions = new OpenIdConnectOptions(authenticationType, loggerFactory);

            // get and apply config from application
            var securitySection = configuration.GetSection(CloudFoundryDefaults.SECURITY_CLIENT_SECTION_PREFIX);
            securitySection.Bind(cloudFoundryOptions);

            // get and apply service binding info
            SsoServiceInfo info = configuration.GetSingletonServiceInfo<SsoServiceInfo>();
            OpenIdConnectConfigurer.Configure(info, cloudFoundryOptions);

            return appBuilder.Use(typeof(OpenIdConnectAuthenticationMiddleware), appBuilder, cloudFoundryOptions);
        }
    }
}

#pragma warning disable SA1403 // File may only contain a single namespace
namespace Steeltoe.Security.Authentication.CloudFoundry
#pragma warning restore SA1403 // File may only contain a single namespace
{
#pragma warning disable SA1402 // File may only contain a single class
    public static class CloudFoundryExtensions
#pragma warning restore SA1402 // File may only contain a single class
    {
        /// <summary>
        /// Configures and adds JWT bearer token middleware to the OWIN request pipeline
        /// </summary>
        /// <param name="appBuilder">Your OWIN AppBuilder</param>
        /// <param name="configuration">Your application configuration</param>
        /// <param name="logger">Include for diagnostic logging during app start</param>
        /// <returns>Your <see cref="IAppBuilder"/></returns>
        public static IAppBuilder UseCloudFoundryJwtBearerAuthentication(this IAppBuilder appBuilder, IConfiguration configuration, ILogger logger = null)
        {
            if (appBuilder == null)
            {
                throw new ArgumentNullException(nameof(appBuilder));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            // get options with defaults
            var cloudFoundryOptions = new CloudFoundryJwtBearerAuthenticationOptions();

            // get and apply config from application
            var securitySection = configuration.GetSection(CloudFoundryDefaults.SECURITY_CLIENT_SECTION_PREFIX);
            securitySection.Bind(cloudFoundryOptions);

            // get and apply service binding info
            SsoServiceInfo si = configuration.GetSingletonServiceInfo<SsoServiceInfo>();
            CloudFoundryJwtOwinConfigurer.Configure(si, cloudFoundryOptions);

            // REVIEW: return without adding auth middleware if no service binding was found... !?
            // - presumably written this way to support local development, but seems like a bad idea
            // - added option to disable, but leaving behavior to default this way, for now, to avoid a breaking change
            if (si == null && cloudFoundryOptions.SkipAuthIfNoBoundSSOService)
            {
                logger?.LogWarning("SSO Service binding not detected, JWT Bearer middleware has not been added!");
                logger?.LogInformation("To include JWT Bearer middleware when bindings aren't found, set security:oauth2:client:SkipAuthIfNoBoundSSOService=false");
                return appBuilder;
            }

            return appBuilder.UseJwtBearerAuthentication(cloudFoundryOptions);
        }
    }
}
