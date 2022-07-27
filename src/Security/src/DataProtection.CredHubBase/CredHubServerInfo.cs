// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Steeltoe.Security.DataProtection.CredHub;

/// <summary>
/// Response object from CredHub /info endpoint
/// </summary>
public class CredHubServerInfo
{
    [JsonPropertyName("auth-server")]
    public Dictionary<string, string> AuthServer { get; set; }

    public Dictionary<string, string> App { get; set; }
}