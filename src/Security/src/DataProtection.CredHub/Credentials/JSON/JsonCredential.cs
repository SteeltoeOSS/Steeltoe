// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Security.DataProtection.CredHub;

[JsonConverter(typeof(JsonCredentialJsonConverter))]
public class JsonCredential : ICredentialValue
{
    public JsonElement Value { get; }

    public JsonCredential(JsonElement value)
    {
        Value = value;
    }

    public JsonCredential(string valueAsString)
    {
        using JsonDocument doc = JsonDocument.Parse(valueAsString);
        Value = doc.RootElement;
    }

    public override string ToString()
    {
        return Value.ToString();
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not JsonCredential other)
        {
            return false;
        }

        return ToString() == other.ToString();
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}
