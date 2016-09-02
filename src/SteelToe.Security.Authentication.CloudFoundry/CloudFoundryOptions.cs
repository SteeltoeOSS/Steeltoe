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
using SteelToe.CloudFoundry.Connector.OAuth;
using System.Runtime.InteropServices;


namespace SteelToe.Security.Authentication.CloudFoundry
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
        public string TokenInfoUrl { get; set; }
        public string JwtKeyUrl { get; set; }
        public bool ValidateCertificates { get; set; } = true;

        public CloudFoundryOptions()
        {
            string authURL = "http://" + Default_OAuthServiceUrl;
            ClaimsIssuer = AUTHENTICATION_SCHEME;
            ClientId = Default_ClientId;
            ClientSecret = Default_ClientSecret;
            AuthenticationScheme = CloudFoundryOptions.AUTHENTICATION_SCHEME;
            DisplayName = CloudFoundryOptions.AUTHENTICATION_SCHEME;
            CallbackPath = new PathString("/signin-cloudfoundry");
            AuthorizationEndpoint = authURL + Default_AuthorizationUri;
            TokenEndpoint = authURL + Default_AccessTokenUri;
            UserInformationEndpoint = authURL + Default_UserInfoUri;
            TokenInfoUrl = authURL + Default_CheckTokenUri;
            JwtKeyUrl = authURL + Default_JwtTokenKey;
            Scope.Clear();
            BackchannelHttpHandler = GetBackChannelHandler();
        }

        public CloudFoundryOptions(OAuthServiceOptions options)
        {
            ClaimsIssuer = AUTHENTICATION_SCHEME;
            ClientId = options.ClientId;
            ClientSecret = options.ClientSecret;
            AuthenticationScheme = CloudFoundryOptions.AUTHENTICATION_SCHEME;
            DisplayName = CloudFoundryOptions.AUTHENTICATION_SCHEME;
            CallbackPath = new PathString("/signin-cloudfoundry");
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

        internal protected virtual HttpMessageHandler GetBackChannelHandler()
        {
#if NET451
            return null;
#else
            if (!ValidateCertificates && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var handler = new WinHttpHandler();
                handler.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                return handler;
            }
            return null;
#endif
        }
    }
}