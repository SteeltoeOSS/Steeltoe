// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;

namespace Steeltoe.Configuration.CloudFoundry;

[DebuggerDisplay("{DebuggerDisplay(),nq}")]
[TypeConverter(typeof(CredentialsConverter))]
public sealed class CloudFoundryCredentials : Dictionary<string, CloudFoundryCredentials>
{
    public string? Value { get; }

    public CloudFoundryCredentials()
    {
    }

    public CloudFoundryCredentials(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        Value = value;
    }

    private string DebuggerDisplay()
    {
        return Value != null ? $"Value = \"{Value}\"" : $"Count = {Count}";
    }
}
