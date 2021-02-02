// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

        /// <summary>
        /// Gets or sets a value indicating whether to validate auth server certificate
        /// </summary>
        public bool Validate_Certificates { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether gets a value indicating whether to validate auth server certificate
        /// </summary>
        public bool ValidateCertificates
        {
            get { return Validate_Certificates; }
            set { Validate_Certificates = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether token issue and expiration times will be set in the auth ticket
        /// </summary>
        public bool UseTokenLifetime { get; set; } = true;

        public CloudFoundryOAuthOptions()
        {
            ClaimsIssuer = CloudFoundryDefaults.AuthenticationScheme;
            ClientId = CloudFoundryDefaults.ClientId;
            ClientSecret = CloudFoundryDefaults.ClientSecret;
            CallbackPath = new PathString(CloudFoundryDefaults.CallbackPath);
            SaveTokens = true;

            ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "user_id");
            ClaimActions.MapJsonKey(ClaimTypes.Name, "user_name");
            ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");
            ClaimActions.MapJsonKey(ClaimTypes.Surname, "family_name");
            ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
            ClaimActions.MapScopes();

            SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

            SetEndpoints("http://" + CloudFoundryDefaults.OAuthServiceUrl);
        }

        public void SetEndpoints(string authDomain)
        {
            if (!string.IsNullOrWhiteSpace(authDomain))
            {
                AuthorizationEndpoint = authDomain + CloudFoundryDefaults.AuthorizationUri;
                TokenEndpoint = authDomain + CloudFoundryDefaults.AccessTokenUri;
                UserInformationEndpoint = authDomain + CloudFoundryDefaults.UserInfoUri;
                TokenInfoUrl = authDomain + CloudFoundryDefaults.CheckTokenUri;
            }
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