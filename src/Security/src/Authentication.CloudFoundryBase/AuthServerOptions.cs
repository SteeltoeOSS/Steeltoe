// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public class AuthServerOptions
    {
        /// <summary>
        /// Gets or sets the location of the OAuth server
        /// </summary>
        public string AuthorizationUrl { get; set; }

        /// <summary>
        /// Gets or sets the location the user is sent to after authentication
        /// </summary>
        public string CallbackUrl { get; set; }

        /// <summary>
        /// Gets or sets the application's client id for interacting with the auth server
        /// </summary>
        public string ClientId { get; set; } = CloudFoundryDefaults.ClientId;

        /// <summary>
        /// Gets or sets the application's client secret for interacting with the auth server
        /// </summary>
        public string ClientSecret { get; set; } = CloudFoundryDefaults.ClientSecret;

        /// <summary>
        /// Gets or sets the name of the authentication type currently in use
        /// </summary>
        public string SignInAsAuthenticationType { get; set; }

        /// <summary>
        /// Gets or sets the timeout (in ms) for calls to the auth server
        /// </summary>
        public int ClientTimeout { get; set; } = 3000;

        /// <summary>
        /// Gets or sets additional scopes beyond 'openid' when requesting tokens
        /// </summary>
        public string AdditionalTokenScopes { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a scopes to require
        /// </summary>
        public string[] RequiredScopes { get; set; }

        /// <summary>
        /// Gets or sets a list of additional audiences to use with token validation
        /// </summary>
        public string[] AdditionalAudiences { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to validate SSO server certificate
        /// </summary>
        public bool ValidateCertificates { get; set; } = true;
    }
}
