// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Security.DataProtection.CredHub
{
    public class PasswordSetRequest : CredentialSetRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordSetRequest"/> class.
        /// </summary>
        /// <param name="credentialName">Name of credential</param>
        /// <param name="password">Value of the credential to set</param>
        public PasswordSetRequest(string credentialName, string password)
        {
            Name = credentialName;
            Type = CredentialType.Password;
            Value = new PasswordCredential(password);
        }
    }
}
