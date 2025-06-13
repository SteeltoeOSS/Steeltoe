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

    /// <summary>
    /// Gets or sets a value indicating whether ANY endpoint starting with /cloudfoundryapplication is accessible. Not to be confused with the accessibility
    /// of the /cloudfoundryapplication hypermedia endpoint.
    /// </summary>
    internal bool IsCloudFoundryEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the Cloud Foundry security middleware has been added to the pipeline.
    /// </summary>
    internal bool HasCloudFoundrySecurity { get; set; }

    /// <summary>
    /// Gets which management endpoints are included and/or excluded.
    /// </summary>
    /// <remarks>
    /// The property value is obtained from configuration keys Management:Endpoints:Web:Exposure (a comma-separated list), falling back to
    /// Management:Endpoints:Actuator:Exposure. So it does NOT bind from Management:Endpoints:Exposure. This property is provided here internally to easily
    /// read the settings.
    /// </remarks>
    internal Exposure Exposure { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether management endpoints are enabled. Default value: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

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
