// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text.Json.Serialization;
using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.CloudFoundry;

internal sealed class SecurityResult
{
    [JsonIgnore]
    public HttpStatusCode Code { get; }

    [JsonIgnore]
    public EndpointPermissions Permissions { get; }

    [JsonPropertyName("security_error")]
    public string Message { get; }

    public SecurityResult(EndpointPermissions permissions)
    {
        Code = HttpStatusCode.OK;
        Message = string.Empty;
        Permissions = permissions;
    }

    public SecurityResult(HttpStatusCode code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
        Message = message;
        Permissions = EndpointPermissions.None;
    }
}
