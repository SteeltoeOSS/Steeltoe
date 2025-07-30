// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Steeltoe.Security.DataProtection.CredHub;

/// <summary>
/// Response object from CredHub /info endpoint
/// </summary>
[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public class CredHubServerInfo
{
    [JsonPropertyName("auth-server")]
    public Dictionary<string, string> AuthServer { get; set; }

    public Dictionary<string, string> App { get; set; }
}