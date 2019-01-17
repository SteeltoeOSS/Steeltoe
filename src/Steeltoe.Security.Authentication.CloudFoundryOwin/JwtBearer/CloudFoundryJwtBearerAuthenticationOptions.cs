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

using Microsoft.Owin.Security.Jwt;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public class CloudFoundryJwtBearerAuthenticationOptions : JwtBearerAuthenticationOptions
    {
        public CloudFoundryJwtBearerAuthenticationOptions()
        {
            string authURL = "http://" + CloudFoundryDefaults.OAuthServiceUrl;
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
