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

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.CloudFoundry.Connector;
using Steeltoe.CloudFoundry.Connector.Services;
using System;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public static class CloudFoundryExtensions
    {
        public static AuthenticationBuilder AddCloudFoundryOAuth(this AuthenticationBuilder builder, IConfiguration config)
        => builder.AddCloudFoundryOAuth(CloudFoundryDefaults.AuthenticationScheme, config);

        public static AuthenticationBuilder AddCloudFoundryOAuth(this AuthenticationBuilder builder, string authenticationScheme, IConfiguration config)
            => builder.AddCloudFoundryOAuth(authenticationScheme, CloudFoundryDefaults.DisplayName, config);

        public static AuthenticationBuilder AddCloudFoundryOAuth(this AuthenticationBuilder builder, string authenticationScheme, string displayName, IConfiguration config)
        {
            builder.AddOAuth<CloudFoundryOAuthOptions, CloudFoundryOAuthHandler>(authenticationScheme, displayName, (options) =>
            {
                var securitySection = config.GetSection(CloudFoundryDefaults.SECURITY_CLIENT_SECTION_PREFIX);
                securitySection.Bind(options);

                SsoServiceInfo info = config.GetSingletonServiceInfo<SsoServiceInfo>();
                CloudFoundryOAuthConfigurer.Configure(info, options);
            });
            return builder;
        }

        public static AuthenticationBuilder AddCloudFoundryOAuth(this AuthenticationBuilder builder, IConfiguration config, Action<CloudFoundryOAuthOptions, IConfiguration> configurer)
            => builder.AddCloudFoundryOAuth(CloudFoundryDefaults.AuthenticationScheme, config, configurer);

        public static AuthenticationBuilder AddCloudFoundryOAuth(this AuthenticationBuilder builder, string authenticationScheme, IConfiguration config, Action<CloudFoundryOAuthOptions, IConfiguration> configurer)
            => builder.AddCloudFoundryOAuth(authenticationScheme, CloudFoundryDefaults.DisplayName, config, configurer);

        public static AuthenticationBuilder AddCloudFoundryOAuth(this AuthenticationBuilder builder, string authenticationScheme, string displayName, IConfiguration config, Action<CloudFoundryOAuthOptions, IConfiguration> configurer)
        {
            builder.AddOAuth<CloudFoundryOAuthOptions, CloudFoundryOAuthHandler>(authenticationScheme, displayName, (options) =>
            {
                configurer(options, config);
            });
            return builder;
        }

        public static AuthenticationBuilder AddCloudFoundryJwtBearer(this AuthenticationBuilder builder, IConfiguration config)
            => builder.AddCloudFoundryJwtBearer(JwtBearerDefaults.AuthenticationScheme, config);

        public static AuthenticationBuilder AddCloudFoundryJwtBearer(this AuthenticationBuilder builder, string authenticationScheme, IConfiguration config)
            => builder.AddCloudFoundryJwtBearer(authenticationScheme, JwtBearerDefaults.AuthenticationScheme, config);

        public static AuthenticationBuilder AddCloudFoundryJwtBearer(this AuthenticationBuilder builder, string authenticationScheme, string displayName, IConfiguration config)
        {
            builder.AddJwtBearer(authenticationScheme, displayName, (options) =>
            {
                var cloudFoundryOptions = new CloudFoundryJwtBearerOptions();
                var securitySection = config.GetSection(CloudFoundryDefaults.SECURITY_CLIENT_SECTION_PREFIX);
                securitySection.Bind(cloudFoundryOptions);

                SsoServiceInfo info = config.GetSingletonServiceInfo<SsoServiceInfo>();
                CloudFoundryJwtBearerConfigurer.Configure(info, options, cloudFoundryOptions);
            });
            return builder;
        }

        public static AuthenticationBuilder AddCloudFoundryJwtBearer(this AuthenticationBuilder builder, IConfiguration config, Action<JwtBearerOptions, IConfiguration> configurer)
            => builder.AddCloudFoundryJwtBearer(JwtBearerDefaults.AuthenticationScheme, config, configurer);

        public static AuthenticationBuilder AddCloudFoundryJwtBearer(this AuthenticationBuilder builder, string authenticationScheme, IConfiguration config, Action<JwtBearerOptions, IConfiguration> configurer)
            => builder.AddCloudFoundryJwtBearer(authenticationScheme, JwtBearerDefaults.AuthenticationScheme, config, configurer);

        public static AuthenticationBuilder AddCloudFoundryJwtBearer(this AuthenticationBuilder builder, string authenticationScheme, string displayName, IConfiguration config, Action<JwtBearerOptions, IConfiguration> configurer)
        {
            builder.AddJwtBearer(authenticationScheme, displayName, (jwtoptions) =>
            {
                configurer(jwtoptions, config);
            });
            return builder;
        }
    }
}
