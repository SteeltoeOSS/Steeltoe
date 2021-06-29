// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;

namespace Steeltoe.Security.DataProtection.CredHub
{
    public class JsonSetRequest : CredentialSetRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSetRequest"/> class.
        /// </summary>
        /// <param name="credentialName">Name of credential</param>
        /// <param name="value">Value of the credential to set</param>
        public JsonSetRequest(string credentialName, JsonElement value)
        {
            Name = credentialName;
            Type = CredentialType.JSON;
            Value = new JsonCredential(value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSetRequest"/> class.
        /// </summary>
        /// <param name="credentialName">Name of credential</param>
        /// <param name="value">Value of the credential to set</param>
        public JsonSetRequest(string credentialName, string value)
        {
            Name = credentialName;
            Type = CredentialType.JSON;
            Value = new JsonCredential(value);
        }
    }
}
