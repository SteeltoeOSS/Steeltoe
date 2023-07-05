// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Steeltoe.Management.Endpoint.Options;

[SuppressMessage("Major Code Smell", "S4004:Collection properties should be readonly", Justification = "Allow in Options")]
public sealed class ManagementEndpointOptions
{
    public bool? Enabled { get; set; }

    public bool? Sensitive { get; set; }

    public string Path { get; set; }

    public string Port { get; set; }

    public IList<HttpMiddlewareOptions> EndpointOptions { get; set; }

    public EndpointContexts EndpointContexts { get; set; } = EndpointContexts.Actuator;

    public bool UseStatusCodeFromResponse { get; set; } = true;

    public JsonSerializerOptions SerializerOptions { get; set; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public Exposure Exposure { get; set; } = new();

    /// <summary>
    /// Gets or sets a list of
    /// <see href="https://docs.microsoft.com/dotnet/api/system.type.assemblyqualifiedname">
    /// assembly-qualified
    /// </see>
    /// custom JsonConverters.
    /// </summary>
    public string[] CustomJsonConverters { get; set; }
}
