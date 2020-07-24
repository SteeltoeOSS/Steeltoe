// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Security.DataProtection.CredHub
{
    public class KeyParameters : ICredentialParameter
    {
        /// <summary>
        /// Gets or sets specify the length of key to be generated
        /// </summary>
        [JsonPropertyName("key_length")]
        public CertificateKeyLength KeyLength { get; set; } = CertificateKeyLength.Length_2048;
    }
}