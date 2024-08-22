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
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Actuators.Loggers;

internal sealed class LoggersEndpointMiddleware : EndpointMiddleware<LoggersRequest, LoggersResponse>
{
    private readonly ILogger<LoggersEndpointMiddleware> _logger;

    public LoggersEndpointMiddleware(ILoggersEndpointHandler endpointHandler, IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        ILoggerFactory loggerFactory)
        : base(endpointHandler, managementOptionsMonitor, loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<LoggersEndpointMiddleware>();
    }

    protected override async Task<LoggersResponse> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        LoggersRequest? loggersRequest = await GetLoggersRequestAsync(context);

        return loggersRequest == null
            ? new LoggersResponse(new Dictionary<string, object>(), true)
            : await EndpointHandler.InvokeAsync(loggersRequest, cancellationToken);
    }

    private async Task<LoggersRequest?> GetLoggersRequestAsync(HttpContext context)
    {
        HttpRequest request = context.Request;

        if (context.Request.Method == "POST")
        {
            // POST - change a logger level
            _logger.LogDebug("Incoming path: {Path}", request.Path.Value);
            string? baseRequestPath = ManagementOptionsMonitor.CurrentValue.GetBaseRequestPath(context.Request);

            string? path = EndpointHandler.Options.Path;

            if (baseRequestPath != null)
            {
                path = baseRequestPath + "/" + path;
                path = path.Replace("//", "/", StringComparison.Ordinal);
            }

            if (request.Path.StartsWithSegments(path, out PathString remaining) && remaining.HasValue)
            {
                string loggerName = remaining.Value!.TrimStart('/');

                Dictionary<string, string> change = await DeserializeRequestAsync(request.Body);

                change.TryGetValue("configuredLevel", out string? level);

                _logger.LogDebug("Change Request: {Name}, {Level}", loggerName, level ?? "RESET");

                if (!string.IsNullOrEmpty(loggerName))
                {
                    if (!string.IsNullOrEmpty(level) && LoggerLevels.StringToLogLevel(level) == null)
                    {
                        _logger.LogDebug("Invalid LogLevel specified: {Level}", level);
                        return null;
                    }

                    return new LoggersRequest(loggerName, level);
                }
            }
        }

        return new LoggersRequest();
    }

    private async Task<Dictionary<string, string>> DeserializeRequestAsync(Stream stream)
    {
        try
        {
            var dictionary = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream);

            if (dictionary != null)
            {
                return dictionary;
            }
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            _logger.LogError(exception, "Exception deserializing loggers endpoint request.");
        }

        return new Dictionary<string, string>();
    }

    protected override async Task WriteResponseAsync(LoggersResponse result, HttpContext context, CancellationToken cancellationToken)
    {
        if (result.HasError)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }
        else
        {
            context.HandleContentNegotiation(_logger);
            JsonSerializerOptions options = ManagementOptionsMonitor.CurrentValue.SerializerOptions;
            await JsonSerializer.SerializeAsync(context.Response.Body, result.Data, options, cancellationToken);
        }
    }
}
