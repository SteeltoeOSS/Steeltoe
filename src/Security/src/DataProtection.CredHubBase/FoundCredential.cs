﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;

namespace Steeltoe.Security.DataProtection.CredHub
{
    /// <summary>
    /// Credential information returned from a Find request
    /// </summary>
    public class FoundCredential
    {
        /// <summary>
        /// Gets or sets full name of credential
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets when this version of the credential was created
        /// </summary>
        [JsonProperty("version_created_at")]
        public string VersionCreatedAt { get; set; }
    }
}
