// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;

namespace Steeltoe.Management.Endpoint.Configuration;

/// <summary>
/// Provides configuration settings for management endpoints (actuators).
/// </summary>
public sealed class ManagementOptions
{
    internal const string UseStatusCodeFromResponseHeaderName = "X-Use-Status-Code-From-Response";

    internal bool IsCloudFoundryEnabled { get; set; }

    /// <summary>
    /// Gets which management endpoints are included and/or excluded.
    /// </summary>
    /// <remarks>
    /// The property value is obtained from configuration keys management:endpoints:web:exposure (a comma-separated list), falling back to
    /// management:endpoints:actuator:exposure. So it does NOT bind from management:endpoints:exposure. This property is provided here internally to easily
    /// read the settings.
    /// </remarks>
    internal Exposure Exposure { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether management endpoints are enabled. Default value: true.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Gets or sets the HTTP request path at which management endpoints are exposed. Default value: /actuator.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the alternate HTTP port at which management endpoints are exposed.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="Port" /> applies to HTTP or HTTPS requests. Default value: false.
    /// </summary>
    public bool SslEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the HTTP response status code is based on the health status. This setting can be overruled by sending an
    /// X-Use-Status-Code-From-Response HTTP header. Default value: true.
    /// </summary>
    public bool UseStatusCodeFromResponse { get; set; } = true;

    /// <summary>
    /// Gets or sets the JSON serialization options.
    /// </summary>
    public JsonSerializerOptions SerializerOptions { get; set; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Gets a list of
    /// <see href="https://docs.microsoft.com/dotnet/api/system.type.assemblyqualifiedname">
    /// assembly-qualified
    /// </see>
    /// custom JSON converters.
    /// </summary>
    public IList<string> CustomJsonConverters { get; } = new List<string>();
}
