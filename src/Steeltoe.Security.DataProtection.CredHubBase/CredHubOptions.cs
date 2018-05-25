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

using System;

namespace Steeltoe.Security.DataProtection.CredHub
{
    /// <summary>
    /// Configured CredHub client
    /// </summary>
    public class CredHubOptions
    {
        /// <summary>
        /// Gets or sets routable address of CredHub server
        /// </summary>
        public string CredHubUrl { get; set; } = "https://credhub.service.cf.internal:8844/api";

        /// <summary>
        /// Gets or sets Client Id for interactions with UAA
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets Client Secret for interactions with UAA
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether validate server certificates for UAA and CredHub servers
        /// </summary>
        public bool ValidateCertificates { get; set; } = true;

        /// <summary>
        /// Perform basic validation to make sure a Client Id and Secret have been provided
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrEmpty(ClientId))
            {
                throw new ArgumentException("A Client Id is required for the CredHub Client");
            }

            if (string.IsNullOrEmpty(ClientSecret))
            {
                throw new ArgumentException("A Client Secret is required for the CredHub Client");
            }
        }
    }
}
