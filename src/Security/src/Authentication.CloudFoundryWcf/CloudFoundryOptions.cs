// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Steeltoe.CloudFoundry.Connector;
using Steeltoe.CloudFoundry.Connector.Services;
using System;
using System.Diagnostics;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf
{
    public class CloudFoundryOptions : AuthServerOptions
    {
        public const string AUTHENTICATION_SCHEME = CloudFoundryDefaults.AuthenticationScheme;
        public const string OAUTH_AUTHENTICATION_SCHEME = "CloudFoundry.OAuth";

        public string TokenInfoUrl => AuthorizationUrl + CloudFoundryDefaults.CheckTokenUri;

        /// <summary>
        /// Gets or sets a value indicating whether the token's audience should be validated
        /// </summary>
        public bool ValidateAudience { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the token's issuer should be validated
        /// </summary>
        public bool ValidateIssuer { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the token's lifetime should be validated
        /// </summary>
        public bool ValidateLifetime { get; set; } = true;

        [Obsolete("This property will be removed in a future release. Use AuthorizationUrl instead")]
        public string OAuthServiceUrl { get => AuthorizationUrl; set => AuthorizationUrl = value; }

        /// <summary>
        /// Gets or sets the authorization endpoint. Default value is /oauth/authorize
        /// </summary>
        public string AuthorizationEndpoint { get; set; } = CloudFoundryDefaults.AuthorizationUri;

        /// <summary>
        /// Gets or sets the Access Token URI. Default value is /oauth/token
        /// </summary>
        public string AccessTokenEndpoint { get; set; } = CloudFoundryDefaults.AccessTokenUri;

        /// <summary>
        /// Gets or sets the User Information Endpoint. Default value is /userinfo
        /// </summary>
        public string UserInformationEndpoint { get; set; } = CloudFoundryDefaults.UserInfoUri;

        [Obsolete("This property will be removed in a future release. Use CloudFoundryDefaults.CheckTokenUri instead")]
        public string TokenInfoEndpoint { get; set; } = "check_token";

        [Obsolete("This property will be removed in a future release. Use CloudFoundryDefaults.JwtTokenKey instead")]
        public string JwtKeyEndpoint { get; set; } = "token_key";

        /// <summary>
        /// Gets or sets a value indicating whether to use app credentials or forward users's credentials
        /// </summary>
        /// <remarks>Setting to true results in passing the user's JWT to downstream services</remarks>
        public bool ForwardUserCredentials { get; set; } = false;

        public TokenValidationParameters TokenValidationParameters { get; set; }

        internal CloudFoundry.CloudFoundryTokenKeyResolver TokenKeyResolver { get; set; }

        internal CloudFoundryWcfTokenValidator TokenValidator { get; set; }

        internal readonly LoggerFactory LoggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFoundryOptions"/> class.
        /// </summary>
        /// <param name="loggerFactory">For logging within the library</param>
        public CloudFoundryOptions(LoggerFactory loggerFactory = null)
        {
            LoggerFactory = loggerFactory;
            AuthorizationUrl = "http://" + CloudFoundryDefaults.OAuthServiceUrl;

            // can't mark this constructor obsolete but want to make these go away
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("sso_auth_domain")) ||
                !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("sso_client_id")) ||
                !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("sso_client_secret")))
            {
                AuthorizationUrl = Environment.GetEnvironmentVariable("sso_auth_domain");
                ClientId = Environment.GetEnvironmentVariable("sso_client_id");
                ClientSecret = Environment.GetEnvironmentVariable("sso_client_secret");

                // not using ILogger because an app using environment variable here could predate inclusion of ILogger in this class
                Console.Error.WriteLine("sso_* variables were detected in your environment! Future releases of Steeltoe will not uses them for configuration");
                Debug.WriteLine("sso_* variables were detected in your environment! Future releases of Steeltoe will not uses them for configuration");
            }

            TokenKeyResolver ??= new CloudFoundry.CloudFoundryTokenKeyResolver(AuthorizationUrl + CloudFoundryDefaults.JwtTokenUri, null, ValidateCertificates);
            TokenValidator ??= new CloudFoundryWcfTokenValidator(this, LoggerFactory?.CreateLogger<CloudFoundryWcfTokenValidator>());
        }

        [Obsolete("This constructor is expected to be removed in a future release. Please reach out if this constructor is important to you!")]
        public CloudFoundryOptions(string authDomain, string clientId, string clientSecret, LoggerFactory loggerFactory = null)
            : this(loggerFactory)
        {
            AuthorizationUrl = authDomain;
            ClientId = clientId;
            ClientSecret = clientSecret;
        }

        [Obsolete("This constructor is expected to be removed in a future release. Please reach out if this constructor is important to you!")]
        public CloudFoundryOptions(string authUrl, LoggerFactory loggerFactory = null)
            : this(loggerFactory)
        {
            AuthorizationUrl = authUrl;
        }

        [Obsolete("This constructor is expected to be removed in a future release. Please reach out if this constructor is important to you!")]
        public CloudFoundryOptions(IConfiguration config)
        {
            var securitySection = config.GetSection(CloudFoundryDefaults.SECURITY_CLIENT_SECTION_PREFIX);
            securitySection.Bind(this);

            SsoServiceInfo info = config.GetSingletonServiceInfo<SsoServiceInfo>();

            AuthorizationUrl = info.AuthDomain;
            ClientId = info.ClientId;
            ClientSecret = info.ClientSecret;
            TokenKeyResolver ??= new CloudFoundry.CloudFoundryTokenKeyResolver(AuthorizationUrl + CloudFoundryDefaults.JwtTokenUri, null, ValidateCertificates);
            TokenValidator ??= new CloudFoundryWcfTokenValidator(this);
            TokenValidationParameters ??= GetTokenValidationParameters();
        }

        internal TokenValidationParameters GetTokenValidationParameters()
        {
            if (TokenValidationParameters != null)
            {
                return TokenValidationParameters;
            }

            var parameters = new TokenValidationParameters();
            TokenKeyResolver ??= new CloudFoundry.CloudFoundryTokenKeyResolver(AuthorizationUrl + CloudFoundryDefaults.JwtTokenUri, null, ValidateCertificates);
            TokenValidator ??= new CloudFoundryWcfTokenValidator(this, LoggerFactory?.CreateLogger<CloudFoundryWcfTokenValidator>());
            TokenValidationParameters = parameters;

            parameters.ValidateAudience = ValidateAudience;
            parameters.AudienceValidator = TokenValidator.ValidateAudience;
            parameters.ValidateIssuer = ValidateIssuer;
            parameters.IssuerSigningKeyResolver = TokenKeyResolver.ResolveSigningKey;
            parameters.ValidateLifetime = ValidateLifetime;
            parameters.IssuerValidator = TokenValidator.ValidateIssuer;

            return parameters;
        }
    }
}