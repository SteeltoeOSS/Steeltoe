// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Steeltoe.Security.DataProtection.CredHub.Credentials.Utilities;

namespace Steeltoe.Security.DataProtection.CredHub.Credentials.Password;

[JsonConverter(typeof(StringCredentialJsonConverter<PasswordCredential>))]
public class PasswordCredential : StringCredential
{
    public PasswordCredential(string value)
        : base(value)
    {
    }
}
