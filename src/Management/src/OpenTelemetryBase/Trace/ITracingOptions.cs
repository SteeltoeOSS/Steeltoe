// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.OpenTelemetry.Trace;

public interface ITracingOptions
{
    /// <summary>
    /// Gets the name of the application.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a regex pattern for requests coming into this application that should not be traced.
    /// </summary>
    /// <remarks>
    /// Default value: "/actuator/.*|/cloudfoundryapplication/.*|.*\\.png|.*\\.css|.*\\.js|.*\\.html|/favicon.ico|/hystrix.stream|.*\\.gif".
    /// </remarks>
    string IngressIgnorePattern { get; }

    /// <summary>
    /// Gets a regex pattern for requests leaving this application that should not be traced.
    /// </summary>
    /// <remarks>
    /// Default value: "/api/v2/spans|/v2/apps/.*/permissions|/eureka/*".
    /// </remarks>
    string EgressIgnorePattern { get; }

    /// <summary>
    /// Gets the maximum payload size in bytes. Default value: 4096.
    /// </summary>
    int MaxPayloadSizeInBytes { get; }

    /// <summary>
    /// Gets a value indicating whether traces should ALWAYS be captured.
    /// </summary>
    bool AlwaysSample { get; }

    /// <summary>
    /// Gets a value indicating whether traces should NEVER be captured.
    /// </summary>
    bool NeverSample { get; }

    /// <summary>
    /// Gets a value indicating whether trace ids should be truncated from 16 to 8 bytes in logs.
    /// </summary>
    /// <remarks>
    /// This setting will NOT affect exported traces.
    /// </remarks>
    bool UseShortTraceIds { get; }

    /// <summary>
    /// Gets a value indicating the propagation format that should be used.
    /// </summary>
    /// <remarks>
    /// Default value is currently B3. W3C trace context is also supported.
    /// </remarks>
    string PropagationType { get; }

    /// <summary>
    /// Gets a value indicating whether one or multiple B3 headers should be used.
    /// </summary>
    /// <remarks>
    /// No effect on other propagation types.
    /// </remarks>
    bool SingleB3Header { get; }

    /// <summary>
    /// Gets a value indicating whether GRPC requests should participate in tracing.
    /// </summary>
    bool EnableGrpcAspNetCoreSupport { get; }

    /// <summary>
    /// Gets a value representing the endpoint used for exporting traces.
    /// </summary>
    Uri ExporterEndpoint { get; }
}
