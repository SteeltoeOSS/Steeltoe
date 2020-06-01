// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
