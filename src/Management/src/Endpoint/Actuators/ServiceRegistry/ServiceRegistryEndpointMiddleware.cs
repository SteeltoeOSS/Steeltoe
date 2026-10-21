// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Extensions;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Actuators.ServiceRegistry;

internal sealed partial class ServiceRegistryEndpointMiddleware(
    IServiceRegistryEndpointHandler endpointHandler, IOptionsMonitor<ManagementOptions> managementOptionsMonitor, ILoggerFactory loggerFactory)
    : EndpointMiddleware<ServiceRegistryRequest, ServiceRegistryResponse>(endpointHandler, managementOptionsMonitor, loggerFactory)
{
    private const string MissingStatusError = "The request body must contain a 'status' property.";

    private readonly ILogger<ServiceRegistryEndpointMiddleware> _logger = loggerFactory.CreateLogger<ServiceRegistryEndpointMiddleware>();

    protected override async Task<ServiceRegistryRequest?> ParseRequestAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (httpContext.Request.Method == "POST")
        {
            Dictionary<string, string?> body = await DeserializeRequestAsync(httpContext.Request.Body, cancellationToken);

            // Returning null results in a bad request.
            return body.TryGetValue("status", out string? status) && !string.IsNullOrWhiteSpace(status) ? new ServiceRegistryRequest(status) : null;
        }

        return new ServiceRegistryRequest();
    }

    private async Task<Dictionary<string, string?>> DeserializeRequestAsync(Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            JsonSerializerOptions options = ManagementOptionsMonitor.CurrentValue.SerializerOptions;
            var dictionary = await JsonSerializer.DeserializeAsync<Dictionary<string, string?>>(stream, options, cancellationToken);

            if (dictionary != null)
            {
                return new Dictionary<string, string?>(dictionary, StringComparer.OrdinalIgnoreCase);
            }
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            LogDeserializationFailed(exception);
        }

        return [];
    }

    protected override async Task<ServiceRegistryResponse> InvokeEndpointHandlerAsync(ServiceRegistryRequest? request, CancellationToken cancellationToken)
    {
        return request == null ? ServiceRegistryResponse.FromError(MissingStatusError) : await EndpointHandler.InvokeAsync(request, cancellationToken);
    }

    protected override async Task WriteResponseAsync(ServiceRegistryResponse response, HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(httpContext);

        if (!response.IsRegistered)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
        }
        else if (response.Error != null)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await httpContext.Response.WriteAsync(response.Error, cancellationToken);
        }
        else if (response.Status != null)
        {
            httpContext.Response.Headers.Append("Content-Type", ContentType);

            JsonSerializerOptions options = ManagementOptionsMonitor.CurrentValue.SerializerOptions;
            await JsonSerializer.SerializeAsync(httpContext.Response.Body, response.Status, options, cancellationToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to deserialize service-registry endpoint request.")]
    private partial void LogDeserializationFailed(Exception exception);
}
