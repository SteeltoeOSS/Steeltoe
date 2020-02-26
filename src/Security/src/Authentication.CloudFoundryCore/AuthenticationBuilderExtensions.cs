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
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector;
using Steeltoe.Connector.Services;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Steeltoe.Security.Authentication.Mtls;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public static class AuthenticationBuilderExtensions
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

                var info = config.GetSingletonServiceInfo<SsoServiceInfo>();
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
        /// <returns><see cref="AuthenticationBuilder"/> configured to use OpenID Connect with UAA or Pivotal SSO</returns>
        public static AuthenticationBuilder AddCloudFoundryOpenIdConnect(this AuthenticationBuilder builder, IConfiguration config)
            => builder.AddCloudFoundryOpenIdConnect(CloudFoundryDefaults.AuthenticationScheme, config);

        /// <summary>
        /// Adds OpenID Connect middleware and configuration for using UAA or Pivotal SSO for user authentication
        /// </summary>
        /// <param name="builder">Your <see cref="AuthenticationBuilder"/></param>
        /// <param name="authenticationScheme">An identifier for this authentication mechanism. Default value is <see cref="CloudFoundryDefaults.DisplayName"/></param>
        /// <param name="config">Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider"/></param>
        /// <returns><see cref="AuthenticationBuilder"/> configured to use OpenID Connect with UAA or Pivotal SSO</returns>
        public static AuthenticationBuilder AddCloudFoundryOpenIdConnect(this AuthenticationBuilder builder, string authenticationScheme, IConfiguration config)
            => builder.AddCloudFoundryOpenIdConnect(authenticationScheme, CloudFoundryDefaults.DisplayName, config);

        /// <summary>
        /// Adds OpenID Connect middleware and configuration for using UAA or Pivotal SSO for user authentication
        /// </summary>
        /// <param name="builder">Your <see cref="AuthenticationBuilder"/></param>
        /// <param name="authenticationScheme">An identifier for this authentication mechanism. Default value is <see cref="CloudFoundryDefaults.DisplayName"/></param>
        /// <param name="displayName">Sets a display name for this auth scheme. Defaults to <see cref="CloudFoundryDefaults.DisplayName"/></param>
        /// <param name="config">Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider"/></param>
        /// <returns><see cref="AuthenticationBuilder"/> configured to use OpenID Connect with UAA or Pivotal SSO</returns>
        public static AuthenticationBuilder AddCloudFoundryOpenIdConnect(this AuthenticationBuilder builder, string authenticationScheme, string displayName, IConfiguration config)
        {
            builder.AddOpenIdConnect(authenticationScheme, displayName, options =>
            {
                var cloudFoundryOptions = new CloudFoundryOpenIdConnectOptions();
                var securitySection = config.GetSection(CloudFoundryDefaults.SECURITY_CLIENT_SECTION_PREFIX);
                securitySection.Bind(cloudFoundryOptions);

                var info = config.GetSingletonServiceInfo<SsoServiceInfo>();
                CloudFoundryOpenIdConnectConfigurer.Configure(info, options, cloudFoundryOptions);
            });
            return builder;
        }

        /// <summary>
        /// Adds OpenID Connect middleware and configuration for using UAA or Pivotal SSO for user authentication
        /// </summary>
        /// <param name="builder">Your <see cref="AuthenticationBuilder"/></param>
        /// <param name="config">Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider"/></param>
        /// <param name="configurer">Configure the <see cref="OpenIdConnectOptions" /> after applying service bindings</param>
        /// <returns><see cref="AuthenticationBuilder"/> configured to use OpenID Connect with UAA or Pivotal SSO</returns>
        public static AuthenticationBuilder AddCloudFoundryOpenIdConnect(this AuthenticationBuilder builder, IConfiguration config, Action<OpenIdConnectOptions, IConfiguration> configurer)
            => builder.AddCloudFoundryOpenIdConnect(CloudFoundryDefaults.AuthenticationScheme, config, configurer);

        /// <summary>
        /// Adds OpenID Connect middleware and configuration for using UAA or Pivotal SSO for user authentication
        /// </summary>
        /// <param name="builder">Your <see cref="AuthenticationBuilder"/></param>
        /// <param name="authenticationScheme">An identifier for this authentication mechanism. Default value is <see cref="CloudFoundryDefaults.DisplayName"/></param>
        /// <param name="config">(Optional) Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider"/></param>
        /// <param name="configurer">Configure the <see cref="OpenIdConnectOptions" /> after applying service bindings</param>
        /// <returns><see cref="AuthenticationBuilder"/> configured to use OpenID Connect with UAA or Pivotal SSO</returns>
        public static AuthenticationBuilder AddCloudFoundryOpenIdConnect(this AuthenticationBuilder builder, string authenticationScheme, IConfiguration config, Action<OpenIdConnectOptions, IConfiguration> configurer)
            => builder.AddCloudFoundryOpenIdConnect(authenticationScheme, CloudFoundryDefaults.DisplayName, config, configurer);

        /// <summary>
        /// Adds OpenID Connect middleware and configuration for using UAA or Pivotal SSO for user authentication
        /// </summary>
        /// <param name="builder">Your <see cref="AuthenticationBuilder"/></param>
        /// <param name="authenticationScheme">An identifier for this authentication mechanism. Default value is <see cref="CloudFoundryDefaults.DisplayName"/></param>
        /// <param name="displayName">Sets a display name for this auth scheme. Defaults to <see cref="CloudFoundryDefaults.DisplayName"/></param>
        /// <param name="config">Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider"/></param>
        /// <param name="configurer">Configure the <see cref="OpenIdConnectOptions" /> after applying service bindings</param>
        /// <returns><see cref="AuthenticationBuilder"/> configured to use OpenID Connect with UAA or Pivotal SSO</returns>
        public static AuthenticationBuilder AddCloudFoundryOpenIdConnect(this AuthenticationBuilder builder, string authenticationScheme, string displayName, IConfiguration config, Action<OpenIdConnectOptions, IConfiguration> configurer)
        {
            builder.AddOpenIdConnect(authenticationScheme, displayName, options =>
            {
                var cloudFoundryOptions = new CloudFoundryOpenIdConnectOptions();
                var securitySection = config.GetSection(CloudFoundryDefaults.SECURITY_CLIENT_SECTION_PREFIX);
                securitySection.Bind(cloudFoundryOptions);

                var info = config.GetSingletonServiceInfo<SsoServiceInfo>();
                CloudFoundryOpenIdConnectConfigurer.Configure(info, options, cloudFoundryOptions);

                configurer(options, config);
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

                var info = config.GetSingletonServiceInfo<SsoServiceInfo>();
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

        /// <summary>
        /// Adds Certificate authentication middleware and configuration to use platform identity certificates
        /// </summary>
        /// <param name="builder">Your <see cref="AuthenticationBuilder"/></param>
        /// <returns><see cref="AuthenticationBuilder"/> configured to use application identity certificates</returns>
        public static AuthenticationBuilder AddCloudFoundryIdentityCertificate(this AuthenticationBuilder builder)
        {
            var logger = builder.Services.BuildServiceProvider().GetService<ILogger<CloudFoundryInstanceCertificate>>();
            builder.AddMutualTls(options =>
            {
                options.Events = new CertificateAuthenticationEvents()
                {
                    OnCertificateValidated = context =>
                    {
                        var claims = new List<Claim>(context.Principal.Claims);
                        if (CloudFoundryInstanceCertificate.TryParse(context.ClientCertificate, out var cfCert, logger))
                        {
                            claims.Add(new Claim(ApplicationClaimTypes.CloudFoundryInstanceId, cfCert.InstanceId, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                            claims.Add(new Claim(ApplicationClaimTypes.CloudFoundryAppId, cfCert.AppId, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                            claims.Add(new Claim(ApplicationClaimTypes.CloudFoundrySpaceId, cfCert.SpaceId, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                            claims.Add(new Claim(ApplicationClaimTypes.CloudFoundryOrgId, cfCert.OrgId, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                        }

                        var identity = new ClaimsIdentity(claims, CertificateAuthenticationDefaults.AuthenticationScheme);
                        context.Principal = new ClaimsPrincipal(identity);
                        context.Success();
                        return Task.CompletedTask;
                    }
                };
            });
            return builder;
        }
    }
}
