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
using Steeltoe.CloudFoundry.Connector;
using Steeltoe.CloudFoundry.Connector.Services;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf
{
    public class CloudFoundryOptions
    {
        public const string AUTHENTICATION_SCHEME = "CloudFoundry";
        public const string OAUTH_AUTHENTICATION_SCHEME = "CloudFoundry.OAuth";

        public string TokenInfoUrl { get; set; }

        public bool ValidateCertificates { get; set; }

        public bool ValidateAudience { get; set; }

        public bool ValidateIssuer { get; set; }

        public bool ValidateLifetime { get; set; }

        public string OAuthServiceUrl { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string AuthorizationEndpoint { get; set; } = "oauth/authorize";

        public string AccessTokenEndpoint { get; set; } = "oauth/token";

        public string UserInformationEndpoint { get; set; } = "userinfo";

        public string TokenInfoEndpoint { get; set; } = "check_token";

        public string JwtKeyEndpoint { get; set; } = "token_key";

        public string[] RequiredScopes { get; set; }

        public string[] AdditionalAudiences { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use app credentials or forward users's credentials
        /// </summary>
        /// <remarks>Setting to true results in passing the user's JWT to downstream services</remarks>
        public bool ForwardUserCredentials { get; set; } = false;

        public TokenValidationParameters TokenValidationParameters { get; set; }

        internal CloudFoundryTokenKeyResolver TokenKeyResolver { get; set; }

        internal CloudFoundryTokenValidator TokenValidator { get; set; }

        public CloudFoundryOptions()
           : this(System.Environment.GetEnvironmentVariable("sso_auth_domain"))
        {
            ClientId = System.Environment.GetEnvironmentVariable("sso_client_id");
            ClientSecret = System.Environment.GetEnvironmentVariable("sso_client_secret");
        }

        public CloudFoundryOptions(string authDomain, string clientId, string clientSecret)
            : this(authDomain)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
        }

        public CloudFoundryOptions(string authUrl)
        {
            // CallbackPath = new PathString("/signin-cloudfoundry");
            OAuthServiceUrl = authUrl;
            ValidateCertificates = false;
            ValidateAudience = true;
            ValidateIssuer = true;
            ValidateLifetime = true;
            TokenKeyResolver = TokenKeyResolver ?? new CloudFoundryTokenKeyResolver(this);
            TokenValidator = TokenValidator ?? new CloudFoundryTokenValidator(this);

            TokenValidationParameters = GetTokenValidationParameters(this);
        }

        public CloudFoundryOptions(IConfiguration config)
        {
            // CloudFoundryDefaults.SECURITY_CLIENT_SECTION_PREFIX
            var securitySection = config.GetSection("security:oauth2:client");
            securitySection.Bind(this);

            SsoServiceInfo info = config.GetSingletonServiceInfo<SsoServiceInfo>();

            OAuthServiceUrl = info.AuthDomain;
            ClientId = info.ClientId;
            ClientSecret = info.ClientSecret;
            TokenKeyResolver = TokenKeyResolver ?? new CloudFoundryTokenKeyResolver(this);
            TokenValidator = TokenValidator ?? new CloudFoundryTokenValidator(this);
            TokenValidationParameters = TokenValidationParameters ?? GetTokenValidationParameters(this);
        }

        internal static TokenValidationParameters GetTokenValidationParameters(CloudFoundryOptions options)
        {
            if (options.TokenValidationParameters != null)
            {
                return options.TokenValidationParameters;
            }

            var parameters = new TokenValidationParameters();
            options.TokenKeyResolver = options.TokenKeyResolver ?? new CloudFoundryTokenKeyResolver(options);
            options.TokenValidator = options.TokenValidator ?? new CloudFoundryTokenValidator(options);
            options.TokenValidationParameters = parameters;

            parameters.ValidateAudience = options.ValidateAudience;
            parameters.ValidateIssuer = options.ValidateIssuer;
            parameters.ValidateLifetime = options.ValidateLifetime;

            parameters.IssuerSigningKeyResolver = options.TokenKeyResolver.ResolveSigningKey;
            parameters.IssuerValidator = options.TokenValidator.ValidateIssuer;
            parameters.AudienceValidator = options.TokenValidator.ValidateAudience;

            return parameters;
        }
    }
}