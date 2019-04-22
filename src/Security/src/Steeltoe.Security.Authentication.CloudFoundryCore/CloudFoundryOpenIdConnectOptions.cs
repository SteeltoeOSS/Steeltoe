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

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public class CloudFoundryOpenIdConnectOptions : OpenIdConnectOptions
    {
        //// https://leastprivilege.com/2017/11/15/missing-claims-in-the-asp-net-core-2-openid-connect-handler/

        public CloudFoundryOpenIdConnectOptions()
        {
            AuthenticationMethod = OpenIdConnectRedirectBehavior.RedirectGet;
            Authority = "https://" + CloudFoundryDefaults.OAuthServiceUrl;
            CallbackPath = new PathString(CloudFoundryDefaults.CallbackPath);
            ClaimsIssuer = CloudFoundryDefaults.AuthenticationScheme;
            ClientId = CloudFoundryDefaults.ClientId;
            ClientSecret = CloudFoundryDefaults.ClientSecret;
            ResponseType = OpenIdConnectResponseType.Code;
            SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            TokenValidationParameters.NameClaimType = "user_name";
        }

        /// <summary>
        /// Gets or sets additional scopes beyond openid and profile when requesting tokens
        /// </summary>
        public string AdditionalScopes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to validate auth server certificate
        /// </summary>
        public bool ValidateCertificates { get; set; } = true;

        internal AuthServerOptions BaseOptions(string updatedClientId)
        {
            return new AuthServerOptions
            {
                ClientId = updatedClientId ?? ClientId,
                ClientSecret = ClientSecret,
                ValidateCertificates = ValidateCertificates,
                AuthorizationUrl = Authority
            };
        }
    }
}
