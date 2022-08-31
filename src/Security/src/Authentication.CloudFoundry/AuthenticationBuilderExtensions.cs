// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Connector;
using Steeltoe.Connector.Services;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Steeltoe.Security.Authentication.Mtls;

namespace Steeltoe.Security.Authentication.CloudFoundry;

public static class AuthenticationBuilderExtensions
{
    /// <summary>
    /// Adds OAuth middleware and configuration for using UAA or Pivotal SSO for user authentication.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="AuthenticationBuilder" />.
    /// </param>
    /// <param name="configuration">
    /// Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider" />.
    /// </param>
    /// <returns>
    /// <see cref="AuthenticationBuilder" /> configured to use OAuth with UAA or Pivotal SSO.
    /// </returns>
    public static AuthenticationBuilder AddCloudFoundryOAuth(this AuthenticationBuilder builder, IConfiguration configuration)
    {
        return builder.AddCloudFoundryOAuth(CloudFoundryDefaults.AuthenticationScheme, configuration);
    }

    /// <summary>
    /// Adds OAuth middleware and configuration for using UAA or Pivotal SSO for user authentication.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="AuthenticationBuilder" />.
    /// </param>
    /// <param name="authenticationScheme">
    /// An identifier for this authentication mechanism.
    /// </param>
    /// <param name="configuration">
    /// Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider" />.
    /// </param>
    /// <returns>
    /// <see cref="AuthenticationBuilder" /> configured to use OAuth with UAA or Pivotal SSO.
    /// </returns>
    public static AuthenticationBuilder AddCloudFoundryOAuth(this AuthenticationBuilder builder, string authenticationScheme, IConfiguration configuration)
    {
        return builder.AddCloudFoundryOAuth(authenticationScheme, CloudFoundryDefaults.DisplayName, configuration);
    }

    /// <summary>
    /// Adds OAuth middleware and configuration for using UAA or Pivotal SSO for user authentication.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="AuthenticationBuilder" />.
    /// </param>
    /// <param name="authenticationScheme">
    /// An identifier for this authentication mechanism.
    /// </param>
    /// <param name="displayName">
    /// Sets a display name for this auth scheme. Defaults to <see cref="CloudFoundryDefaults.DisplayName" />.
    /// </param>
    /// <param name="configuration">
    /// Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider" />.
    /// </param>
    /// <returns>
    /// <see cref="AuthenticationBuilder" /> configured to use OAuth with UAA or Pivotal SSO.
    /// </returns>
    public static AuthenticationBuilder AddCloudFoundryOAuth(this AuthenticationBuilder builder, string authenticationScheme, string displayName,
        IConfiguration configuration)
    {
        builder.AddOAuth<CloudFoundryOAuthOptions, CloudFoundryOAuthHandler>(authenticationScheme, displayName, options =>
        {
            IConfigurationSection securitySection = configuration.GetSection(CloudFoundryDefaults.SecurityClientSectionPrefix);
            securitySection.Bind(options);
            options.SetEndpoints(GetAuthDomain(securitySection));

            var info = configuration.GetSingletonServiceInfo<SsoServiceInfo>();
            CloudFoundryOAuthConfigurer.Configure(info, options);
        });

        return builder;
    }

    /// <summary>
    /// Adds OAuth middleware and configuration for using UAA or Pivotal SSO for user authentication.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="AuthenticationBuilder" />.
    /// </param>
    /// <param name="configuration">
    /// Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider" />.
    /// </param>
    /// <param name="configurer">
    /// Used to configure the options.
    /// </param>
    /// <returns>
    /// <see cref="AuthenticationBuilder" /> configured to use OAuth with UAA or Pivotal SSO.
    /// </returns>
    public static AuthenticationBuilder AddCloudFoundryOAuth(this AuthenticationBuilder builder, IConfiguration configuration,
        Action<CloudFoundryOAuthOptions, IConfiguration> configurer)
    {
        return builder.AddCloudFoundryOAuth(CloudFoundryDefaults.AuthenticationScheme, configuration, configurer);
    }

    /// <summary>
    /// Adds OAuth middleware and configuration for using UAA or Pivotal SSO for user authentication.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="AuthenticationBuilder" />.
    /// </param>
    /// <param name="authenticationScheme">
    /// An identifier for this authentication mechanism. Default value is <see cref="CloudFoundryDefaults.DisplayName" />.
    /// </param>
    /// <param name="configuration">
    /// Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider" />.
    /// </param>
    /// <param name="configurer">
    /// Used to configure the options.
    /// </param>
    /// <returns>
    /// <see cref="AuthenticationBuilder" /> configured to use OAuth with UAA or Pivotal SSO.
    /// </returns>
    public static AuthenticationBuilder AddCloudFoundryOAuth(this AuthenticationBuilder builder, string authenticationScheme, IConfiguration configuration,
        Action<CloudFoundryOAuthOptions, IConfiguration> configurer)
    {
        return builder.AddCloudFoundryOAuth(authenticationScheme, CloudFoundryDefaults.DisplayName, configuration, configurer);
    }

    /// <summary>
    /// Adds OAuth middleware and configuration for using UAA or Pivotal SSO for user authentication.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="AuthenticationBuilder" />.
    /// </param>
    /// <param name="authenticationScheme">
    /// An identifier for this authentication mechanism. Default value is <see cref="CloudFoundryDefaults.DisplayName" />.
    /// </param>
    /// <param name="displayName">
    /// Sets a display name for this auth scheme. Defaults to <see cref="CloudFoundryDefaults.DisplayName" />.
    /// </param>
    /// <param name="configuration">
    /// Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider" />.
    /// </param>
    /// <param name="configurer">
    /// Used to configure the options.
    /// </param>
    /// <returns>
    /// <see cref="AuthenticationBuilder" /> configured to use OAuth with UAA or Pivotal SSO.
    /// </returns>
    public static AuthenticationBuilder AddCloudFoundryOAuth(this AuthenticationBuilder builder, string authenticationScheme, string displayName,
        IConfiguration configuration, Action<CloudFoundryOAuthOptions, IConfiguration> configurer)
    {
        builder.AddOAuth<CloudFoundryOAuthOptions, CloudFoundryOAuthHandler>(authenticationScheme, displayName, options =>
        {
            configurer(options, configuration);
        });

        return builder;
    }

    /// <summary>
    /// Adds OpenID Connect middleware and configuration for using UAA or Pivotal SSO for user authentication.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="AuthenticationBuilder" />.
    /// </param>
    /// <param name="configuration">
    /// Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider" />.
    /// </param>
    /// <returns>
    /// <see cref="AuthenticationBuilder" /> configured to use OpenID Connect with UAA or Pivotal SSO.
    /// </returns>
    public static AuthenticationBuilder AddCloudFoundryOpenIdConnect(this AuthenticationBuilder builder, IConfiguration configuration)
    {
        return builder.AddCloudFoundryOpenIdConnect(CloudFoundryDefaults.AuthenticationScheme, configuration);
    }

    /// <summary>
    /// Adds OpenID Connect middleware and configuration for using UAA or Pivotal SSO for user authentication.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="AuthenticationBuilder" />.
    /// </param>
    /// <param name="authenticationScheme">
    /// An identifier for this authentication mechanism. Default value is <see cref="CloudFoundryDefaults.DisplayName" />.
    /// </param>
    /// <param name="configuration">
    /// Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider" />.
    /// </param>
    /// <returns>
    /// <see cref="AuthenticationBuilder" /> configured to use OpenID Connect with UAA or Pivotal SSO.
    /// </returns>
    public static AuthenticationBuilder AddCloudFoundryOpenIdConnect(this AuthenticationBuilder builder, string authenticationScheme,
        IConfiguration configuration)
    {
        return builder.AddCloudFoundryOpenIdConnect(authenticationScheme, CloudFoundryDefaults.DisplayName, configuration);
    }

    /// <summary>
    /// Adds OpenID Connect middleware and configuration for using UAA or Pivotal SSO for user authentication.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="AuthenticationBuilder" />.
    /// </param>
    /// <param name="authenticationScheme">
    /// An identifier for this authentication mechanism. Default value is <see cref="CloudFoundryDefaults.DisplayName" />.
    /// </param>
    /// <param name="displayName">
    /// Sets a display name for this auth scheme. Defaults to <see cref="CloudFoundryDefaults.DisplayName" />.
    /// </param>
    /// <param name="configuration">
    /// Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider" />.
    /// </param>
    /// <returns>
    /// <see cref="AuthenticationBuilder" /> configured to use OpenID Connect with UAA or Pivotal SSO.
    /// </returns>
    public static AuthenticationBuilder AddCloudFoundryOpenIdConnect(this AuthenticationBuilder builder, string authenticationScheme, string displayName,
        IConfiguration configuration)
    {
        builder.AddOpenIdConnect(authenticationScheme, displayName, options =>
        {
            var cloudFoundryOptions = new CloudFoundryOpenIdConnectOptions();
            IConfigurationSection securitySection = configuration.GetSection(CloudFoundryDefaults.SecurityClientSectionPrefix);
            securitySection.Bind(cloudFoundryOptions);

            var info = configuration.GetSingletonServiceInfo<SsoServiceInfo>();
            CloudFoundryOpenIdConnectConfigurer.Configure(info, options, cloudFoundryOptions);
        });

        return builder;
    }

    /// <summary>
    /// Adds OpenID Connect middleware and configuration for using UAA or Pivotal SSO for user authentication.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="AuthenticationBuilder" />.
    /// </param>
    /// <param name="configuration">
    /// Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider" />.
    /// </param>
    /// <param name="configurer">
    /// Configure the <see cref="OpenIdConnectOptions" /> after applying service bindings.
    /// </param>
    /// <returns>
    /// <see cref="AuthenticationBuilder" /> configured to use OpenID Connect with UAA or Pivotal SSO.
    /// </returns>
    public static AuthenticationBuilder AddCloudFoundryOpenIdConnect(this AuthenticationBuilder builder, IConfiguration configuration,
        Action<OpenIdConnectOptions, IConfiguration> configurer)
    {
        return builder.AddCloudFoundryOpenIdConnect(CloudFoundryDefaults.AuthenticationScheme, configuration, configurer);
    }

    /// <summary>
    /// Adds OpenID Connect middleware and configuration for using UAA or Pivotal SSO for user authentication.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="AuthenticationBuilder" />.
    /// </param>
    /// <param name="authenticationScheme">
    /// An identifier for this authentication mechanism. Default value is <see cref="CloudFoundryDefaults.DisplayName" />.
    /// </param>
    /// <param name="configuration">
    /// (Optional) Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider" />.
    /// </param>
    /// <param name="configurer">
    /// Configure the <see cref="OpenIdConnectOptions" /> after applying service bindings.
    /// </param>
    /// <returns>
    /// <see cref="AuthenticationBuilder" /> configured to use OpenID Connect with UAA or Pivotal SSO.
    /// </returns>
    public static AuthenticationBuilder AddCloudFoundryOpenIdConnect(this AuthenticationBuilder builder, string authenticationScheme,
        IConfiguration configuration, Action<OpenIdConnectOptions, IConfiguration> configurer)
    {
        return builder.AddCloudFoundryOpenIdConnect(authenticationScheme, CloudFoundryDefaults.DisplayName, configuration, configurer);
    }

    /// <summary>
    /// Adds OpenID Connect middleware and configuration for using UAA or Pivotal SSO for user authentication.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="AuthenticationBuilder" />.
    /// </param>
    /// <param name="authenticationScheme">
    /// An identifier for this authentication mechanism. Default value is <see cref="CloudFoundryDefaults.DisplayName" />.
    /// </param>
    /// <param name="displayName">
    /// Sets a display name for this auth scheme. Defaults to <see cref="CloudFoundryDefaults.DisplayName" />.
    /// </param>
    /// <param name="configuration">
    /// Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider" />.
    /// </param>
    /// <param name="configurer">
    /// Configure the <see cref="OpenIdConnectOptions" /> after applying service bindings.
    /// </param>
    /// <returns>
    /// <see cref="AuthenticationBuilder" /> configured to use OpenID Connect with UAA or Pivotal SSO.
    /// </returns>
    public static AuthenticationBuilder AddCloudFoundryOpenIdConnect(this AuthenticationBuilder builder, string authenticationScheme, string displayName,
        IConfiguration configuration, Action<OpenIdConnectOptions, IConfiguration> configurer)
    {
        builder.AddOpenIdConnect(authenticationScheme, displayName, options =>
        {
            var cloudFoundryOptions = new CloudFoundryOpenIdConnectOptions();
            IConfigurationSection securitySection = configuration.GetSection(CloudFoundryDefaults.SecurityClientSectionPrefix);
            securitySection.Bind(cloudFoundryOptions);

            var info = configuration.GetSingletonServiceInfo<SsoServiceInfo>();
            CloudFoundryOpenIdConnectConfigurer.Configure(info, options, cloudFoundryOptions);

            configurer(options, configuration);
        });

        return builder;
    }

    /// <summary>
    /// Adds JWT middleware and configuration for using UAA or Pivotal SSO for bearer token authentication.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="AuthenticationBuilder" />.
    /// </param>
    /// <param name="configuration">
    /// Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider" />.
    /// </param>
    /// <returns>
    /// <see cref="AuthenticationBuilder" /> configured to use JWT Bearer tokens from UAA or Pivotal SSO.
    /// </returns>
    public static AuthenticationBuilder AddCloudFoundryJwtBearer(this AuthenticationBuilder builder, IConfiguration configuration)
    {
        return builder.AddCloudFoundryJwtBearer(JwtBearerDefaults.AuthenticationScheme, configuration);
    }

    /// <summary>
    /// Adds JWT middleware and configuration for using UAA or Pivotal SSO for bearer token authentication.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="AuthenticationBuilder" />.
    /// </param>
    /// <param name="authenticationScheme">
    /// An identifier for this authentication mechanism. Default value is <see cref="JwtBearerDefaults.AuthenticationScheme" />.
    /// </param>
    /// <param name="configuration">
    /// Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider" />.
    /// </param>
    /// <returns>
    /// <see cref="AuthenticationBuilder" /> configured to use JWT Bearer tokens from UAA or Pivotal SSO.
    /// </returns>
    public static AuthenticationBuilder AddCloudFoundryJwtBearer(this AuthenticationBuilder builder, string authenticationScheme, IConfiguration configuration)
    {
        return builder.AddCloudFoundryJwtBearer(authenticationScheme, JwtBearerDefaults.AuthenticationScheme, configuration);
    }

    /// <summary>
    /// Adds JWT middleware and configuration for using UAA or Pivotal SSO for bearer token authentication.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="AuthenticationBuilder" />.
    /// </param>
    /// <param name="authenticationScheme">
    /// An identifier for this authentication mechanism. Default value is <see cref="JwtBearerDefaults.AuthenticationScheme" />.
    /// </param>
    /// <param name="displayName">
    /// Sets a display name for this auth scheme. Defaults to <see cref="JwtBearerDefaults.AuthenticationScheme" />.
    /// </param>
    /// <param name="configuration">
    /// Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider" />.
    /// </param>
    /// <returns>
    /// <see cref="AuthenticationBuilder" /> configured to use JWT Bearer tokens from UAA or Pivotal SSO.
    /// </returns>
    public static AuthenticationBuilder AddCloudFoundryJwtBearer(this AuthenticationBuilder builder, string authenticationScheme, string displayName,
        IConfiguration configuration)
    {
        builder.AddJwtBearer(authenticationScheme, displayName, options =>
        {
            var cloudFoundryOptions = new CloudFoundryJwtBearerOptions();
            IConfigurationSection securitySection = configuration.GetSection(CloudFoundryDefaults.SecurityClientSectionPrefix);
            securitySection.Bind(cloudFoundryOptions);
            cloudFoundryOptions.SetEndpoints(GetAuthDomain(securitySection));

            var info = configuration.GetSingletonServiceInfo<SsoServiceInfo>();
            CloudFoundryJwtBearerConfigurer.Configure(info, options, cloudFoundryOptions);
        });

        return builder;
    }

    /// <summary>
    /// Adds JWT middleware and configuration for using UAA or Pivotal SSO for bearer token authentication.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="AuthenticationBuilder" />.
    /// </param>
    /// <param name="configuration">
    /// Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider" />.
    /// </param>
    /// <param name="configurer">
    /// Used to configure the options.
    /// </param>
    /// <returns>
    /// <see cref="AuthenticationBuilder" /> configured to use JWT Bearer tokens from UAA or Pivotal SSO.
    /// </returns>
    public static AuthenticationBuilder AddCloudFoundryJwtBearer(this AuthenticationBuilder builder, IConfiguration configuration,
        Action<JwtBearerOptions, IConfiguration> configurer)
    {
        return builder.AddCloudFoundryJwtBearer(JwtBearerDefaults.AuthenticationScheme, configuration, configurer);
    }

    /// <summary>
    /// Adds JWT middleware and configuration for using UAA or Pivotal SSO for bearer token authentication.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="AuthenticationBuilder" />.
    /// </param>
    /// <param name="authenticationScheme">
    /// An identifier for this authentication mechanism. Default value is <see cref="JwtBearerDefaults.AuthenticationScheme" />.
    /// </param>
    /// <param name="configuration">
    /// Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider" />.
    /// </param>
    /// <param name="configurer">
    /// Used to configure the options.
    /// </param>
    /// <returns>
    /// <see cref="AuthenticationBuilder" /> configured to use JWT Bearer tokens from UAA or Pivotal SSO.
    /// </returns>
    public static AuthenticationBuilder AddCloudFoundryJwtBearer(this AuthenticationBuilder builder, string authenticationScheme, IConfiguration configuration,
        Action<JwtBearerOptions, IConfiguration> configurer)
    {
        return builder.AddCloudFoundryJwtBearer(authenticationScheme, JwtBearerDefaults.AuthenticationScheme, configuration, configurer);
    }

    /// <summary>
    /// Adds JWT middleware and configuration for using UAA or Pivotal SSO for bearer token authentication.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="AuthenticationBuilder" />.
    /// </param>
    /// <param name="authenticationScheme">
    /// An identifier for this authentication mechanism. Default value is <see cref="JwtBearerDefaults.AuthenticationScheme" />.
    /// </param>
    /// <param name="displayName">
    /// Sets a display name for this auth scheme. Defaults to <see cref="JwtBearerDefaults.AuthenticationScheme" />.
    /// </param>
    /// <param name="configuration">
    /// Your application configuration. Be sure to include the <see cref="CloudFoundryConfigurationProvider" />.
    /// </param>
    /// <param name="configurer">
    /// Used to configure the options.
    /// </param>
    /// <returns>
    /// <see cref="AuthenticationBuilder" /> configured to use JWT Bearer tokens from UAA or Pivotal SSO.
    /// </returns>
    public static AuthenticationBuilder AddCloudFoundryJwtBearer(this AuthenticationBuilder builder, string authenticationScheme, string displayName,
        IConfiguration configuration, Action<JwtBearerOptions, IConfiguration> configurer)
    {
        builder.AddJwtBearer(authenticationScheme, displayName, jwtOptions =>
        {
            configurer(jwtOptions, configuration);
        });

        return builder;
    }

    /// <summary>
    /// Adds Certificate authentication middleware and configuration to use platform identity certificates.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="AuthenticationBuilder" />.
    /// </param>
    /// <returns>
    /// <see cref="AuthenticationBuilder" /> configured to use application identity certificates.
    /// </returns>
    public static AuthenticationBuilder AddCloudFoundryIdentityCertificate(this AuthenticationBuilder builder)
    {
        return builder.AddCloudFoundryIdentityCertificate(CertificateAuthenticationDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Adds Certificate authentication middleware and configuration to use platform identity certificates.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="AuthenticationBuilder" />.
    /// </param>
    /// <param name="configurer">
    /// Used to configure the options.
    /// </param>
    /// <returns>
    /// <see cref="AuthenticationBuilder" /> configured to use application identity certificates.
    /// </returns>
    public static AuthenticationBuilder AddCloudFoundryIdentityCertificate(this AuthenticationBuilder builder,
        Action<MutualTlsAuthenticationOptions> configurer)
    {
        return builder.AddCloudFoundryIdentityCertificate(CertificateAuthenticationDefaults.AuthenticationScheme, configurer);
    }

    /// <summary>
    /// Adds Certificate authentication middleware and configuration to use platform identity certificates.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="AuthenticationBuilder" />.
    /// </param>
    /// <param name="authenticationScheme">
    /// An identifier for this authentication mechanism. Default value is <see cref="JwtBearerDefaults.AuthenticationScheme" />.
    /// </param>
    /// <returns>
    /// <see cref="AuthenticationBuilder" /> configured to use application identity certificates.
    /// </returns>
    public static AuthenticationBuilder AddCloudFoundryIdentityCertificate(this AuthenticationBuilder builder, string authenticationScheme)
    {
        return builder.AddCloudFoundryIdentityCertificate(authenticationScheme, null);
    }

    /// <summary>
    /// Adds Certificate authentication middleware and configuration to use platform identity certificates.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="AuthenticationBuilder" />.
    /// </param>
    /// <param name="authenticationScheme">
    /// An identifier for this authentication mechanism. Default value is <see cref="CertificateAuthenticationDefaults.AuthenticationScheme" />.
    /// </param>
    /// <param name="configurer">
    /// Used to configure the options.
    /// </param>
    /// <returns>
    /// <see cref="AuthenticationBuilder" /> configured to use application identity certificates.
    /// </returns>
    public static AuthenticationBuilder AddCloudFoundryIdentityCertificate(this AuthenticationBuilder builder, string authenticationScheme,
        Action<MutualTlsAuthenticationOptions> configurer)
    {
        builder.AddMutualTls(authenticationScheme, options =>
        {
            configurer?.Invoke(options);
        });

        return builder;
    }

    private static string GetAuthDomain(IConfigurationSection configurationSection)
    {
        return configurationSection.GetValue<string>(nameof(SsoServiceInfo.AuthDomain));
    }
}
