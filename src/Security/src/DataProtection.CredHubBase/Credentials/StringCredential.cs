// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Security.DataProtection.CredHub;

[JsonConverter(typeof(StringCredentialJsonConverter<StringCredential>))]
public class StringCredential : ICredentialValue
{
    private readonly string _value;

    public StringCredential(string value)
    {
        _value = value;
    }

    public override string ToString()
    {
        return _value;
    }
}
