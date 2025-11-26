// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings.ResponseTypes;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappings;

/// <summary>
/// Middleware for displaying the mapped ASP.NET Core endpoints.
/// </summary>
internal sealed class RouteMappingsEndpointMiddleware(
    IRouteMappingsEndpointHandler endpointHandler, IOptionsMonitor<ManagementOptions> managementOptionsMonitor, ILoggerFactory loggerFactory)
    : EndpointMiddleware<object?, RouteMappingsResponse>(endpointHandler, managementOptionsMonitor, loggerFactory)
{
    private JsonSerializerOptions? _serializerOptions;

    protected override async Task<RouteMappingsResponse> InvokeEndpointHandlerAsync(object? request, CancellationToken cancellationToken)
    {
        return await EndpointHandler.InvokeAsync(request, cancellationToken);
    }

    protected override async Task WriteResponseAsync(RouteMappingsResponse response, HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (Equals(response, null))
        {
            return;
        }

        httpContext.Response.Headers.Append("Content-Type", ContentType);

        // Use UnsafeRelaxedJsonEscaping to make generic method signatures human-readable (e.g., backticks in generic type names).
        _serializerOptions ??= new JsonSerializerOptions(ManagementOptionsMonitor.CurrentValue.SerializerOptions)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        await JsonSerializer.SerializeAsync(httpContext.Response.Body, response, _serializerOptions, cancellationToken);
    }
}
