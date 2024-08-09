// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Tracing;

public sealed class TracingOptions
{
    /// <summary>
    /// Gets or sets the name of this application.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a regular expression for requests coming into this application that should not be traced. Default value:
    /// <code><![CDATA[
    /// "/actuator/.*|/cloudfoundryapplication/.*|.*\\.png|.*\\.css|.*\\.js|.*\\.html|/favicon.ico|.*\\.gif"
    /// ]]></code>
    /// </summary>
    public string? IngressIgnorePattern { get; set; }

    /// <summary>
    /// Gets or sets a regular expression for requests leaving this application that should not be traced. Default value:
    /// <code><![CDATA[
    /// "/api/v2/spans|/v2/apps/.*/permissions|/eureka/*"
    /// ]]></code>
    /// </summary>
    public string? EgressIgnorePattern { get; set; }

    /// <summary>
    /// Gets or sets the maximum payload size in bytes. Default value: 4096.
    /// </summary>
    public int MaxPayloadSizeInBytes { get; set; } = 4096;

    /// <summary>
    /// Gets or sets a value indicating whether traces should ALWAYS be captured. Default value: false.
    /// </summary>
    public bool AlwaysSample { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether traces should NEVER be captured. Default value: false.
    /// </summary>
    public bool NeverSample { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether trace IDs should be truncated from 16 to 8 bytes in logs. Default value: false.
    /// </summary>
    /// <remarks>
    /// This setting will NOT affect exported traces.
    /// </remarks>
    public bool UseShortTraceIds { get; set; }

    /// <summary>
    /// Gets or sets the propagation format that should be used. Default value: B3.
    /// </summary>
    public string? PropagationType { get; set; } = "B3";

    /// <summary>
    /// Gets or sets a value indicating whether one or multiple B3 headers should be used. Default value: true.
    /// </summary>
    public bool SingleB3Header { get; set; } = true;

    /// <summary>
    /// Gets or sets the endpoint used for exporting traces.
    /// </summary>
    public Uri? ExporterEndpoint { get; set; }
}
