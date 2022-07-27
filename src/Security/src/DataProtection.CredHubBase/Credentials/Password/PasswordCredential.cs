// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Security.DataProtection.CredHub;

[JsonConverter(typeof(StringCredentialJsonConverter<PasswordCredential>))]
public class PasswordCredential : StringCredential
{
    public PasswordCredential(string value)
        : base(value)
    {
    }
}