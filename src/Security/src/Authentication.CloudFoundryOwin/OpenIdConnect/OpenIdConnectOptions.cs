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

using Microsoft.Extensions.Logging;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using System;

namespace Steeltoe.Security.Authentication.CloudFoundry.Owin
{
    public class OpenIdConnectOptions : AuthenticationOptions
    {
        public OpenIdConnectOptions(string authenticationType = CloudFoundryDefaults.DisplayName, LoggerFactory loggerFactory = null)
           : base(authenticationType)
        {
            Description.Caption = authenticationType;
            AuthenticationMode = AuthenticationMode.Passive;
            LoggerFactory = loggerFactory;
        }

        public PathString CallbackPath { get; set; } = new PathString(CloudFoundryDefaults.CallbackPath);

        public string AuthDomain { get; set; } = "http://" + CloudFoundryDefaults.OAuthServiceUrl;

        public string ClientId { get; set; } = CloudFoundryDefaults.ClientId;

        [Obsolete("This property will be removed in a future release, use ClientId instead")]
        public string ClientID
        {
            get { return ClientId; }
            set { ClientId = value; }
        }

        public string ClientSecret { get; set; } = CloudFoundryDefaults.ClientSecret;

        public string AppHost { get; set; }

        public int AppPort { get; set; }

        public string SignInAsAuthenticationType { get; set; }

        /// <summary>
        /// Gets or sets additional scopes beyond 'openid' when requesting tokens
        /// </summary>
        public string AdditionalScopes { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether to validate SSO server certificate
        /// </summary>
        public bool ValidateCertificates { get; set; } = true;

        public string TokenInfoUrl => AuthDomain + CloudFoundryDefaults.CheckTokenUri;

        public ISecureDataFormat<AuthenticationProperties> StateDataFormat { get; set; }

        public AuthServerOptions AsAuthServerOptions(string callbackUrl = null)
        {
            return new AuthServerOptions
            {
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                AdditionalTokenScopes = AdditionalScopes,
                ValidateCertificates = ValidateCertificates,
                SignInAsAuthenticationType = SignInAsAuthenticationType,
                AuthorizationUrl = AuthDomain + CloudFoundryDefaults.AccessTokenUri,
                CallbackUrl = callbackUrl ?? "https://" + AppHost + (AppPort != 0 ? ":" + AppPort : string.Empty) + CallbackPath
            };
        }

        internal ILoggerFactory LoggerFactory;
    }
}
