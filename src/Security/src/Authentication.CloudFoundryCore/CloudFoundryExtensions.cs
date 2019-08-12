// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
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
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public static class CloudFoundryExtensions
    {
        /// <summary>
        /// Adds OAuth middleware and configuration for using UAA or Pivotal SSO for user authentication
        /// </summary>
        /// <param name="builder">Your <see cref="AuthenticationBuilder"/></param>
        /// <param name="config">Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider"/></param>
        /// <returns><see cref="AuthenticationBuilder"/> configured to use OAuth with UAA or Pivotal SSO</returns>
        public static AuthenticationBuilder AddCloudFoundryOAuth(this AuthenticationBuilder builder, IConfiguration config)
        => builder.AddCloudFoundryOAuth(CloudFoundryDefaults.AuthenticationScheme, config);

        /// <summary>
        /// Adds OAuth middleware and configuration for using UAA or Pivotal SSO for user authentication
        /// </summary>
        /// <param name="builder">Your <see cref="AuthenticationBuilder"/></param>
        /// <param name="authenticationScheme">An identifier for this authentication mechanism.</param>
        /// <param name="config">Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider"/></param>
        /// <returns><see cref="AuthenticationBuilder"/> configured to use OAuth with UAA or Pivotal SSO</returns>
        public static AuthenticationBuilder AddCloudFoundryOAuth(this AuthenticationBuilder builder, string authenticationScheme, IConfiguration config)
            => builder.AddCloudFoundryOAuth(authenticationScheme, CloudFoundryDefaults.DisplayName, config);

        /// <summary>
        /// Adds OAuth middleware and configuration for using UAA or Pivotal SSO for user authentication
        /// </summary>
        /// <param name="builder">Your <see cref="AuthenticationBuilder"/></param>
        /// <param name="authenticationScheme">An identifier for this authentication mechanism.</param>
        /// <param name="displayName">Sets a display name for this auth scheme. Defaults to <see cref="CloudFoundryDefaults.DisplayName"/></param>
        /// <param name="config">Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider"/></param>
        /// <returns><see cref="AuthenticationBuilder"/> configured to use OAuth with UAA or Pivotal SSO</returns>
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

        /// <summary>
        /// Adds OAuth middleware and configuration for using UAA or Pivotal SSO for user authentication
        /// </summary>
        /// <param name="builder">Your <see cref="AuthenticationBuilder"/></param>
        /// <param name="config">Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider"/></param>
        /// <param name="configurer">Used to configure the options</param>
        /// <returns><see cref="AuthenticationBuilder"/> configured to use OAuth with UAA or Pivotal SSO</returns>
        public static AuthenticationBuilder AddCloudFoundryOAuth(this AuthenticationBuilder builder, IConfiguration config, Action<CloudFoundryOAuthOptions, IConfiguration> configurer)
            => builder.AddCloudFoundryOAuth(CloudFoundryDefaults.AuthenticationScheme, config, configurer);

        /// <summary>
        /// Adds OAuth middleware and configuration for using UAA or Pivotal SSO for user authentication
        /// </summary>
        /// <param name="builder">Your <see cref="AuthenticationBuilder"/></param>
        /// <param name="authenticationScheme">An identifier for this authentication mechanism. Default value is <see cref="CloudFoundryDefaults.DisplayName"/></param>
        /// <param name="config">Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider"/></param>
        /// <param name="configurer">Used to configure the options</param>
        /// <returns><see cref="AuthenticationBuilder"/> configured to use OAuth with UAA or Pivotal SSO</returns>
        public static AuthenticationBuilder AddCloudFoundryOAuth(this AuthenticationBuilder builder, string authenticationScheme, IConfiguration config, Action<CloudFoundryOAuthOptions, IConfiguration> configurer)
            => builder.AddCloudFoundryOAuth(authenticationScheme, CloudFoundryDefaults.DisplayName, config, configurer);

        /// <summary>
        /// Adds OAuth middleware and configuration for using UAA or Pivotal SSO for user authentication
        /// </summary>
        /// <param name="builder">Your <see cref="AuthenticationBuilder"/></param>
        /// <param name="authenticationScheme">An identifier for this authentication mechanism. Default value is <see cref="CloudFoundryDefaults.DisplayName"/></param>
        /// <param name="displayName">Sets a display name for this auth scheme. Defaults to <see cref="CloudFoundryDefaults.DisplayName"/></param>
        /// <param name="config">Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider"/></param>
        /// <param name="configurer">Used to configure the options</param>
        /// <returns><see cref="AuthenticationBuilder"/> configured to use OAuth with UAA or Pivotal SSO</returns>
        public static AuthenticationBuilder AddCloudFoundryOAuth(this AuthenticationBuilder builder, string authenticationScheme, string displayName, IConfiguration config, Action<CloudFoundryOAuthOptions, IConfiguration> configurer)
        {
            builder.AddOAuth<CloudFoundryOAuthOptions, CloudFoundryOAuthHandler>(authenticationScheme, displayName, (options) =>
            {
                configurer(options, config);
            });
            return builder;
        }

        /// <summary>
        /// Adds OpenID Connect middleware and configuration for using UAA or Pivotal SSO for user authentication
        /// </summary>
        /// <param name="builder">Your <see cref="AuthenticationBuilder"/></param>
        /// <param name="config">Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider"/></param>
        /// <param name="cloudFoundryOIDCOptions">Your own <see cref="CloudFoundryOpenIdConnectOptions"/></param>
        /// <returns><see cref="AuthenticationBuilder"/> configured to use OpenID Connect with UAA or Pivotal SSO</returns>
        public static AuthenticationBuilder AddCloudFoundryOpenIdConnect(this AuthenticationBuilder builder, IConfiguration config, CloudFoundryOpenIdConnectOptions cloudFoundryOIDCOptions = null)
            => builder.AddCloudFoundryOpenIdConnect(CloudFoundryDefaults.AuthenticationScheme, config, cloudFoundryOIDCOptions);

        /// <summary>
        /// Adds OpenID Connect middleware and configuration for using UAA or Pivotal SSO for user authentication
        /// </summary>
        /// <param name="builder">Your <see cref="AuthenticationBuilder"/></param>
        /// <param name="authenticationScheme">An identifier for this authentication mechanism. Default value is <see cref="CloudFoundryDefaults.DisplayName"/></param>
        /// <param name="config">(Optional) Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider"/></param>
        /// <param name="cloudFoundryOIDCOptions">(Optional) Your own <see cref="CloudFoundryOpenIdConnectOptions"/></param>
        /// <returns><see cref="AuthenticationBuilder"/> configured to use OpenID Connect with UAA or Pivotal SSO</returns>
        public static AuthenticationBuilder AddCloudFoundryOpenIdConnect(this AuthenticationBuilder builder, string authenticationScheme, IConfiguration config, CloudFoundryOpenIdConnectOptions cloudFoundryOIDCOptions = null)
            => builder.AddCloudFoundryOpenIdConnect(authenticationScheme, CloudFoundryDefaults.DisplayName, config, cloudFoundryOIDCOptions);

        /// <summary>
        /// Adds OpenID Connect middleware and configuration for using UAA or Pivotal SSO for user authentication
        /// </summary>
        /// <param name="builder">Your <see cref="AuthenticationBuilder"/></param>
        /// <param name="authenticationScheme">An identifier for this authentication mechanism. Default value is <see cref="CloudFoundryDefaults.DisplayName"/></param>
        /// <param name="displayName">Sets a display name for this auth scheme. Defaults to <see cref="CloudFoundryDefaults.DisplayName"/></param>
        /// <param name="config">Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider"/></param>
        /// <param name="cloudFoundryOIDCOptions">(Optional) Your own <see cref="CloudFoundryOpenIdConnectOptions"/></param>
        /// <returns><see cref="AuthenticationBuilder"/> configured to use OpenID Connect with UAA or Pivotal SSO</returns>
        public static AuthenticationBuilder AddCloudFoundryOpenIdConnect(this AuthenticationBuilder builder, string authenticationScheme, string displayName, IConfiguration config, CloudFoundryOpenIdConnectOptions cloudFoundryOIDCOptions = null)
        {
            cloudFoundryOIDCOptions = cloudFoundryOIDCOptions ?? new CloudFoundryOpenIdConnectOptions();

            builder.AddOpenIdConnect(authenticationScheme, displayName, options =>
            {
                var securitySection = config.GetSection(CloudFoundryDefaults.SECURITY_CLIENT_SECTION_PREFIX);
                securitySection.Bind(cloudFoundryOIDCOptions);

                SsoServiceInfo info = config.GetSingletonServiceInfo<SsoServiceInfo>();
                CloudFoundryOpenIdConnectConfigurer.Configure(info, options, cloudFoundryOIDCOptions);
            });
            return builder;
        }

        /// <summary>
        /// Adds JWT middleware and configuration for using UAA or Pivotal SSO for bearer token authentication
        /// </summary>
        /// <param name="builder">Your <see cref="AuthenticationBuilder"/></param>
        /// <param name="config">Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider"/></param>
        /// <returns><see cref="AuthenticationBuilder"/> configured to use JWT Bearer tokens from UAA or Pivotal SSO</returns>
        public static AuthenticationBuilder AddCloudFoundryJwtBearer(this AuthenticationBuilder builder, IConfiguration config)
            => builder.AddCloudFoundryJwtBearer(JwtBearerDefaults.AuthenticationScheme, config);

        /// <summary>
        /// Adds JWT middleware and configuration for using UAA or Pivotal SSO for bearer token authentication
        /// </summary>
        /// <param name="builder">Your <see cref="AuthenticationBuilder"/></param>
        /// <param name="authenticationScheme">An identifier for this authentication mechanism. Default value is <see cref="JwtBearerDefaults.AuthenticationScheme"/></param>
        /// <param name="config">Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider"/></param>
        /// <returns><see cref="AuthenticationBuilder"/> configured to use JWT Bearer tokens from UAA or Pivotal SSO</returns>
        public static AuthenticationBuilder AddCloudFoundryJwtBearer(this AuthenticationBuilder builder, string authenticationScheme, IConfiguration config)
            => builder.AddCloudFoundryJwtBearer(authenticationScheme, JwtBearerDefaults.AuthenticationScheme, config);

        /// <summary>
        /// Adds JWT middleware and configuration for using UAA or Pivotal SSO for bearer token authentication
        /// </summary>
        /// <param name="builder">Your <see cref="AuthenticationBuilder"/></param>
        /// <param name="authenticationScheme">An identifier for this authentication mechanism. Default value is <see cref="JwtBearerDefaults.AuthenticationScheme"/></param>
        /// <param name="displayName">Sets a display name for this auth scheme. Defaults to <see cref="JwtBearerDefaults.AuthenticationScheme"/></param>
        /// <param name="config">Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider"/></param>
        /// <returns><see cref="AuthenticationBuilder"/> configured to use JWT Bearer tokens from UAA or Pivotal SSO</returns>
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

        /// <summary>
        /// Adds JWT middleware and configuration for using UAA or Pivotal SSO for bearer token authentication
        /// </summary>
        /// <param name="builder">Your <see cref="AuthenticationBuilder"/></param>
        /// <param name="config">Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider"/></param>
        /// <param name="configurer">Used to configure the options</param>
        /// <returns><see cref="AuthenticationBuilder"/> configured to use JWT Bearer tokens from UAA or Pivotal SSO</returns>
        public static AuthenticationBuilder AddCloudFoundryJwtBearer(this AuthenticationBuilder builder, IConfiguration config, Action<JwtBearerOptions, IConfiguration> configurer)
            => builder.AddCloudFoundryJwtBearer(JwtBearerDefaults.AuthenticationScheme, config, configurer);

        /// <summary>
        /// Adds JWT middleware and configuration for using UAA or Pivotal SSO for bearer token authentication
        /// </summary>
        /// <param name="builder">Your <see cref="AuthenticationBuilder"/></param>
        /// <param name="authenticationScheme">An identifier for this authentication mechanism. Default value is <see cref="JwtBearerDefaults.AuthenticationScheme"/></param>
        /// <param name="config">Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider"/></param>
        /// <param name="configurer">Used to configure the options</param>
        /// <returns><see cref="AuthenticationBuilder"/> configured to use JWT Bearer tokens from UAA or Pivotal SSO</returns>
        public static AuthenticationBuilder AddCloudFoundryJwtBearer(this AuthenticationBuilder builder, string authenticationScheme, IConfiguration config, Action<JwtBearerOptions, IConfiguration> configurer)
            => builder.AddCloudFoundryJwtBearer(authenticationScheme, JwtBearerDefaults.AuthenticationScheme, config, configurer);

        /// <summary>
        /// Adds JWT middleware and configuration for using UAA or Pivotal SSO for bearer token authentication
        /// </summary>
        /// <param name="builder">Your <see cref="AuthenticationBuilder"/></param>
        /// <param name="authenticationScheme">An identifier for this authentication mechanism. Default value is <see cref="JwtBearerDefaults.AuthenticationScheme"/></param>
        /// <param name="displayName">Sets a display name for this auth scheme. Defaults to <see cref="JwtBearerDefaults.AuthenticationScheme"/></param>
        /// <param name="config">Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider"/></param>
        /// <param name="configurer">Used to configure the options</param>
        /// <returns><see cref="AuthenticationBuilder"/> configured to use JWT Bearer tokens from UAA or Pivotal SSO</returns>
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
