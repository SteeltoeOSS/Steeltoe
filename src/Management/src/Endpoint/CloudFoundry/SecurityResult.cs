// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

public class SecurityResult
{
    [JsonIgnore]
    public HttpStatusCode Code { get; }

    [JsonIgnore]
    public Permissions Permissions { get; }

    [JsonPropertyName("security_error")]
    public string Message { get; }

    public SecurityResult(Permissions level)
    {
        Code = HttpStatusCode.OK;
        Message = string.Empty;
        Permissions = level;
    }

    public SecurityResult(HttpStatusCode code, string message)
    {
        Code = code;
        Message = message;
        Permissions = Permissions.None;
    }
}
