// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.ManagementPort;

/// <summary>
/// Blocks access to actuator endpoints on ports other than the management port. Blocks access to non-actuator endpoints on the management port.
/// </summary>
internal sealed class ManagementPortMiddleware
{
    private readonly IOptionsMonitor<ManagementOptions> _managementOptionsMonitor;
    private readonly RequestDelegate? _next;
    private readonly ILogger<ManagementPortMiddleware> _logger;

    public ManagementPortMiddleware(IOptionsMonitor<ManagementOptions> managementOptionsMonitor, RequestDelegate? next,
        ILogger<ManagementPortMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(managementOptionsMonitor);
        ArgumentNullException.ThrowIfNull(logger);

        _managementOptionsMonitor = managementOptionsMonitor;
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ManagementOptions managementOptions = _managementOptionsMonitor.CurrentValue;
        _logger.LogDebug("InvokeAsync({RequestPath}), OptionsPath: {OptionsPath}", context.Request.Path.Value, managementOptions.Path);

        bool allowRequest = IsRequestAllowed(context.Request, managementOptions);

        if (!allowRequest)
        {
            SetResponseError(context, managementOptions.Port);
        }
        else
        {
            if (_next != null)
            {
                await _next(context);
            }
        }
    }

    private bool IsRequestAllowed(HttpRequest request, ManagementOptions managementOptions)
    {
        if (managementOptions.Port is > 0 and < 65536)
        {
            bool isManagementPath = request.Path.StartsWithSegments(managementOptions.Path);
            bool isManagementScheme = managementOptions.SslEnabled ? request.Scheme == Uri.UriSchemeHttps : request.Scheme == Uri.UriSchemeHttp;
            bool isManagementPort = request.Host.Port == managementOptions.Port || HasMappedInstancePort(managementOptions.Port, request.Host.Port);

            return isManagementPath ? isManagementScheme && isManagementPort : !isManagementScheme || !isManagementPort;
        }

        return true;
    }

    // Evaluate CF_INSTANCE_PORTS for port forwarding scenarios where the Host HTTP header doesn't match the actual socket connection.
    private bool HasMappedInstancePort(int managementPort, int? requestPort)
    {
        string? instancePorts = Environment.GetEnvironmentVariable("CF_INSTANCE_PORTS");

        if (!string.IsNullOrEmpty(instancePorts))
        {
            var portMappings = JsonSerializer.Deserialize<List<PortMapping>>(instancePorts);

            PortMapping? portMapping = portMappings?.Find(mapping =>
                mapping.Internal == managementPort && (requestPort == mapping.ExternalTlsProxy || requestPort == mapping.InternalTlsProxy));

            if (portMapping != null)
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace(
                        "Request received on port {RequestPort}. Allowed by CF_INSTANCE_PORTS mapping: [ Internal: {InternalPort}, ExternalTlsProxy: {ExternalTlsProxy}, InternalTlsProxy: {InternalTlsProxy} ]",
                        requestPort, portMapping.Internal, portMapping.ExternalTlsProxy, portMapping.InternalTlsProxy);
                }

                return true;
            }
        }

        return false;
    }

    private void SetResponseError(HttpContext context, int managementPort)
    {
        int? defaultPort = null;

        if (context.Request.Host.Port == null)
        {
            defaultPort = context.Request.Scheme == "http" ? 80 : 443;
        }

        _logger.LogWarning("Access to {Path} on port {Port} denied because 'Management:Endpoints:Port' is set to {ManagementPort}.", context.Request.Path,
            defaultPort ?? context.Request.Host.Port, managementPort);

        context.Response.StatusCode = StatusCodes.Status404NotFound;
    }

    private sealed record PortMapping
    {
        [JsonPropertyName("internal")]
        public int? Internal { get; init; }

        [JsonPropertyName("external_tls_proxy")]
        public int? ExternalTlsProxy { get; init; }

        [JsonPropertyName("internal_tls_proxy")]
        public int? InternalTlsProxy { get; init; }
    }
}
