// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Security.DataProtection.CredHub.Credentials.Utilities;
using System.Text.Json.Serialization;

namespace Steeltoe.Security.DataProtection.CredHub
{
    [JsonConverter(typeof(SetRequestJsonConverter))]
    public class CredentialSetRequest : CredHubBaseObject
    {
        /// <summary>
        /// Gets or sets value of the credential to be set
        /// </summary>
        public ICredentialValue Value { get; set; }
    }
}
