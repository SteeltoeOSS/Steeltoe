// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Security.DataProtection.CredHub
{
    public class RsaSetRequest : CredentialSetRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RsaSetRequest"/> class.
        /// </summary>
        /// <param name="credentialName">Name of credential</param>
        /// <param name="privateKey">Private key for the credential</param>
        /// <param name="publicKey">Public key for the credential</param>
        public RsaSetRequest(string credentialName, string privateKey, string publicKey)
        {
            Name = credentialName;
            Type = CredentialType.RSA;
            Value = new RsaCredential { PrivateKey = privateKey, PublicKey = publicKey };
        }
    }
}
