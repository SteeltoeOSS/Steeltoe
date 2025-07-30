// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Security.DataProtection.CredHub;

[JsonConverter(typeof(JsonCredentialJsonConverter))]
[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public class JsonCredential : ICredentialValue
{
    public JsonCredential(JsonElement value)
    {
        Value = value;
    }

    public JsonCredential(string valueAsString)
    {
        using var doc = JsonDocument.Parse(valueAsString);
        Value = doc.RootElement;
    }

    public JsonElement Value { get; private set; }

    public override string ToString()
    {
        return Value.ToString();
    }

    public override bool Equals(object obj)
    {
        return Value.ToString() == obj.ToString();
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}