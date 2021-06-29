// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Security.DataProtection.CredHub
{
    public abstract class KeyPairCredential : ICredentialValue
    {
        /// <summary>
        /// Gets or sets public key for a credential
        /// </summary>
        [JsonPropertyName("public_key")]
        public string PublicKey { get; set; }

        /// <summary>
        /// Gets or sets private key for a certificate
        /// </summary>
        [JsonPropertyName("private_key")]
        public string PrivateKey { get; set; }
    }
}
