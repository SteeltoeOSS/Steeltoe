// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Steeltoe.Security.DataProtection.CredHub
{
    /// <summary>
    /// Return object from bulk certificate regeneration request
    /// </summary>
    public class RegeneratedCertificates
    {
        /// <summary>
        /// Gets or sets names of certificates that were regenerated
        /// </summary>
        [JsonProperty("regenerated_credentials")]
        public List<string> RegeneratedCredentials { get; set; }
    }
}
