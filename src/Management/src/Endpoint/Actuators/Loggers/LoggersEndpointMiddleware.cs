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

namespace Steeltoe.Management.Endpoint.Actuators.Loggers;

internal sealed partial class LoggersEndpointMiddleware(
    ILoggersEndpointHandler endpointHandler, IOptionsMonitor<ManagementOptions> managementOptionsMonitor, ILoggerFactory loggerFactory)
    : EndpointMiddleware<LoggersRequest, LoggersResponse?>(endpointHandler, managementOptionsMonitor, loggerFactory)
{
    private readonly ILogger<LoggersEndpointMiddleware> _logger = loggerFactory.CreateLogger<LoggersEndpointMiddleware>();

    protected override async Task<LoggersRequest?> ParseRequestAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (httpContext.Request.Method == "POST")
        {
            // POST - change a logger level
            string? basePath = ManagementOptionsMonitor.CurrentValue.GetBasePath(httpContext.Request.Path);
            string path = EndpointOptions.GetEndpointPath(basePath);

            if (httpContext.Request.Path.StartsWithSegments(path, out PathString remaining) && remaining.HasValue)
            {
                string loggerName = remaining.Value.TrimStart('/');

                Dictionary<string, string?> changes = await DeserializeRequestAsync(httpContext.Request.Body, cancellationToken);

                if (changes.TryGetValue("configuredLevel", out string? level))
                {
                    LogChangeRequest(loggerName, level ?? "RESET");

                    if (!string.IsNullOrEmpty(loggerName))
                    {
                        if (!string.IsNullOrEmpty(level) && LoggerLevels.StringToLogLevel(level) == null)
                        {
                            LogInvalidLevel(level);
                            return null;
                        }

                        return new LoggersRequest(loggerName, level);
                    }
                }
            }
        }

        return new LoggersRequest();
    }

    private async Task<Dictionary<string, string?>> DeserializeRequestAsync(Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            JsonSerializerOptions options = ManagementOptionsMonitor.CurrentValue.SerializerOptions;
            var dictionary = await JsonSerializer.DeserializeAsync<Dictionary<string, string?>>(stream, options, cancellationToken);

            if (dictionary != null)
            {
                return dictionary;
            }
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            LogDeserializationFailed(exception);
        }

        return [];
    }

    protected override async Task<LoggersResponse?> InvokeEndpointHandlerAsync(LoggersRequest? request, CancellationToken cancellationToken)
    {
        return request == null ? LoggersResponse.Error : await EndpointHandler.InvokeAsync(request, cancellationToken);
    }

    protected override async Task WriteResponseAsync(LoggersResponse? response, HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (response is { HasError: true })
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }
        else if (response == null)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
        else
        {
            httpContext.Response.Headers.Append("Content-Type", ContentType);

            JsonSerializerOptions options = ManagementOptionsMonitor.CurrentValue.SerializerOptions;
            await JsonSerializer.SerializeAsync(httpContext.Response.Body, response, options, cancellationToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Received request to change logger '{Name}' to level {Level}.")]
    private partial void LogChangeRequest(string name, string level);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Invalid log level {Level} specified.")]
    private partial void LogInvalidLevel(string level);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to deserialize loggers endpoint request.")]
    private partial void LogDeserializationFailed(Exception exception);
}
