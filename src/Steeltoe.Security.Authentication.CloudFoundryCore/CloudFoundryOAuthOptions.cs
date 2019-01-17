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
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public class CloudFoundryOAuthOptions : OAuthOptions
    {
        public string TokenInfoUrl { get; set; }

        public bool Validate_Certificates { get; set; } = true;

        public bool ValidateCertificates => Validate_Certificates;

        public bool UseTokenLifetime { get; set; } = true;

        public CloudFoundryOAuthOptions()
        {
            string authURL = "http://" + CloudFoundryDefaults.OAuthServiceUrl;
            ClaimsIssuer = CloudFoundryDefaults.AuthenticationScheme;
            ClientId = CloudFoundryDefaults.ClientId;
            ClientSecret = CloudFoundryDefaults.ClientSecret;
            CallbackPath = new PathString(CloudFoundryDefaults.CallbackPath);
            AuthorizationEndpoint = authURL + CloudFoundryDefaults.AuthorizationUri;
            TokenEndpoint = authURL + CloudFoundryDefaults.AccessTokenUri;
            UserInformationEndpoint = authURL + CloudFoundryDefaults.UserInfoUri;
            TokenInfoUrl = authURL + CloudFoundryDefaults.CheckTokenUri;
            SaveTokens = true;

            ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "user_id");
            ClaimActions.MapJsonKey(ClaimTypes.Name, "user_name");
            ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");
            ClaimActions.MapJsonKey(ClaimTypes.Surname, "family_name");
            ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
            ClaimActions.MapScopes();

            SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        }

        internal AuthServerOptions BaseOptions()
        {
            return new AuthServerOptions
            {
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                ValidateCertificates = ValidateCertificates,
                AuthorizationUrl = AuthorizationEndpoint
            };
        }
    }
}