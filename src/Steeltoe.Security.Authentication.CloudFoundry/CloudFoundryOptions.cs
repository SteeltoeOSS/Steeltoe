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

using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Steeltoe.CloudFoundry.Connector.OAuth;
using Microsoft.IdentityModel.Tokens;

namespace Steeltoe.Security.Authentication.CloudFoundry
{ 
 
    public class CloudFoundryOptions : OAuthOptions
    {
        internal const string Default_AuthorizationUri = "/oauth/authorize";
        internal const string Default_AccessTokenUri = "/oauth/token";
        internal const string Default_UserInfoUri = "/userinfo";
        internal const string Default_CheckTokenUri = "/check_token";
        internal const string Default_JwtTokenKey = "/token_key";
        internal const string Default_OAuthServiceUrl = "Default_OAuthServiceUrl";
        internal const string Default_ClientId = "Default_ClientId";
        internal const string Default_ClientSecret = "Default_ClientSecret";

        public const string AUTHENTICATION_SCHEME = "CloudFoundry";
        public const string OAUTH_AUTHENTICATION_SCHEME = "CloudFoundry.OAuth";

        public string TokenInfoUrl { get; set; }
        public string JwtKeyUrl { get; set; }
        public bool ValidateCertificates { get; set; } = true;
        public PathString AccessDeniedPath { get; set; }
        public PathString LogoutPath { get; set; }
        public JwtBearerOptions JwtBearerOptions { get; set; }
        public TokenValidationParameters TokenValidationParameters { get; set; }
        internal CloudFoundryTokenKeyResolver TokenKeyResolver { get; set; } 
        internal CloudFoundryTokenValidator TokenValidator { get; set; }

        public CloudFoundryOptions()
        {
            string authURL = "http://" + Default_OAuthServiceUrl;
            ClaimsIssuer = AUTHENTICATION_SCHEME;
            ClientId = Default_ClientId;
            ClientSecret = Default_ClientSecret;
            AuthenticationScheme = CloudFoundryOptions.OAUTH_AUTHENTICATION_SCHEME;
            DisplayName = CloudFoundryOptions.AUTHENTICATION_SCHEME;
            CallbackPath = new PathString("/signin-cloudfoundry");
            AuthorizationEndpoint = authURL + Default_AuthorizationUri;
            TokenEndpoint = authURL + Default_AccessTokenUri;
            UserInformationEndpoint = authURL + Default_UserInfoUri;
            TokenInfoUrl = authURL + Default_CheckTokenUri;
            JwtKeyUrl = authURL + Default_JwtTokenKey;
            SaveTokens = true;
            AutomaticChallenge = true;
            Scope.Clear();

        }

        public CloudFoundryOptions(OAuthServiceOptions options) : this()
        {
            ClientId = options.ClientId;
            ClientSecret = options.ClientSecret;
            AuthorizationEndpoint = options.UserAuthorizationUrl;
            TokenEndpoint = options.AccessTokenUrl;
            UserInformationEndpoint = options.UserInfoUrl;
            TokenInfoUrl = options.TokenInfoUrl;
            JwtKeyUrl = options.JwtKeyUrl;
            AutomaticChallenge = true;

            foreach (var scope in options.Scope)
            {
                Scope.Add(scope);
            }

            BackchannelHttpHandler = GetBackChannelHandler();
        }

        internal protected virtual HttpMessageHandler GetBackChannelHandler()
        {
#if NET46
            return null;
#else
            if (!ValidateCertificates)
            {
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                return handler;
            }
            return null;
#endif
        }

        internal protected virtual void UpdateOptions(OAuthServiceOptions options)
        {
            if (options == null || options.ClientId == null ||
                options.ClientId.Equals(OAuthConnectorDefaults.Default_ClientId))
            {
                return;
            }

            ClientId = options.ClientId;
            ClientSecret = options.ClientSecret;
            AuthorizationEndpoint = options.UserAuthorizationUrl;
            TokenEndpoint = options.AccessTokenUrl;
            UserInformationEndpoint = options.UserInfoUrl;
            TokenInfoUrl = options.TokenInfoUrl;
            JwtKeyUrl = options.JwtKeyUrl;
            ValidateCertificates = options.ValidateCertificates;

            foreach (var scope in options.Scope)
            {
                Scope.Add(scope);
            }

            BackchannelHttpHandler = GetBackChannelHandler();
        }
    }
}