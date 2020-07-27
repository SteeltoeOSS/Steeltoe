// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Owin.Security.Jwt;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public class CloudFoundryJwtBearerAuthenticationOptions : JwtBearerAuthenticationOptions
    {
        public CloudFoundryJwtBearerAuthenticationOptions()
        {
            var authURL = "http://" + CloudFoundryDefaults.OAuthServiceUrl;
            JwtKeyUrl = authURL + CloudFoundryDefaults.JwtTokenUri;
        }

        /// <summary>
        /// Gets or sets a value indicating whether auth middleware is added if no service binding is found
        /// </summary>
        /// <remarks>Is set to 'true' for compatibility with releases prior to Steeltoe 2.2.</remarks>
        public bool SkipAuthIfNoBoundSSOService { get; set; } = true;

        public string JwtKeyUrl { get; set; }

        public bool ValidateCertificates { get; set; } = true;
    }
}
