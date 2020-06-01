// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public class CloudFoundryJwtBearerOptions : JwtBearerOptions
    {
        public CloudFoundryJwtBearerOptions()
        {
            string authURL = "http://" + CloudFoundryDefaults.OAuthServiceUrl;
            ClaimsIssuer = CloudFoundryDefaults.AuthenticationScheme;
            JwtKeyUrl = authURL + CloudFoundryDefaults.JwtTokenUri;
            SaveToken = true;
            TokenValidationParameters.ValidateAudience = false;
            TokenValidationParameters.ValidateIssuer = true;
            TokenValidationParameters.ValidateLifetime = true;
        }

        public string JwtKeyUrl { get; set; }

        public bool Validate_Certificates { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether gets a value indicating whether to validate auth server certificate
        /// </summary>
        public bool ValidateCertificates
        {
            get { return Validate_Certificates; }
            set { Validate_Certificates = value; }
        }
    }
}
