// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

namespace Steeltoe.Configuration;

[TypeConverter(typeof(CredentialConverter))]
public sealed class Credential : Dictionary<string, Credential>
{
    public string? Value { get; }

    public Credential()
    {
    }

    public Credential(string value)
    {
        Value = value;
    }
}
