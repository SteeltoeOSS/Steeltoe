// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Loggers;

public class LoggersEndpointMiddleware : EndpointMiddleware<Dictionary<string, object>, LoggersChangeRequest>
{
    private readonly RequestDelegate _next;

    public LoggersEndpointMiddleware(RequestDelegate next, LoggersEndpoint endpoint, IManagementOptions managementOptions,
        ILogger<LoggersEndpointMiddleware> logger = null)
        : base(endpoint, managementOptions, logger)
    {
        _next = next;
    }

    public Task Invoke(HttpContext context)
    {
        if (endpoint.ShouldInvoke(managementOptions, logger))
        {
            return HandleLoggersRequestAsync(context);
        }

        return Task.CompletedTask;
    }

    protected internal async Task HandleLoggersRequestAsync(HttpContext context)
    {
        HttpRequest request = context.Request;
        HttpResponse response = context.Response;

        if (context.Request.Method.Equals("POST"))
        {
            // POST - change a logger level
            var paths = new List<string>();
            logger?.LogDebug("Incoming path: {0}", request.Path.Value);
            paths.Add(managementOptions == null ? endpoint.Path : $"{managementOptions.Path}/{endpoint.Path}".Replace("//", "/"));

            foreach (string path in paths.Distinct())
            {
                if (ChangeLoggerLevel(request, path))
                {
                    response.StatusCode = (int)HttpStatusCode.OK;
                    return;
                }
            }

            response.StatusCode = (int)HttpStatusCode.BadRequest;
            return;
        }

        // GET request
        string serialInfo = HandleRequest(null);
        logger?.LogDebug("Returning: {0}", serialInfo);

        context.HandleContentNegotiation(logger);
        await context.Response.WriteAsync(serialInfo).ConfigureAwait(false);
    }

    private bool ChangeLoggerLevel(HttpRequest request, string path)
    {
        var epPath = new PathString(path);

        if (request.Path.StartsWithSegments(epPath, out PathString remaining) && remaining.HasValue)
        {
            string loggerName = remaining.Value.TrimStart('/');

            Dictionary<string, string> change = ((LoggersEndpoint)endpoint).DeserializeRequest(request.Body);

            change.TryGetValue("configuredLevel", out string level);

            logger?.LogDebug("Change Request: {0}, {1}", loggerName, level ?? "RESET");

            if (!string.IsNullOrEmpty(loggerName))
            {
                if (!string.IsNullOrEmpty(level) && LoggerLevels.MapLogLevel(level) == null)
                {
                    logger?.LogDebug("Invalid LogLevel specified: {0}", level);
                }
                else
                {
                    var changeReq = new LoggersChangeRequest(loggerName, level);
                    HandleRequest(changeReq);
                    return true;
                }
            }
        }

        return false;
    }
}
