// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Loggers;

public class LoggersEndpointMiddleware : EndpointMiddleware<Dictionary<string, object>, LoggersChangeRequest>
{
    public LoggersEndpointMiddleware( /*RequestDelegate next,*/ LoggersEndpoint endpoint, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        ILogger<LoggersEndpointMiddleware> logger = null)
        : base(endpoint, managementOptions, logger)
    {
    }

    public override Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        return Endpoint.Options.ShouldInvoke(managementOptions, context, logger) ? HandleLoggersRequestAsync(context) : Task.CompletedTask;
    }

    protected internal async Task HandleLoggersRequestAsync(HttpContext context)
    {
        HttpRequest request = context.Request;
        HttpResponse response = context.Response;

        if (context.Request.Method == "POST")
        {
            // POST - change a logger level
            logger?.LogDebug("Incoming path: {path}", request.Path.Value);
            ManagementEndpointOptions mgmtOptions = managementOptions.GetCurrentContext(request.Path);

            string path = managementOptions == null
                ? Endpoint.Options.Path
                : $"{mgmtOptions.Path}/{Endpoint.Options.Path}".Replace("//", "/", StringComparison.Ordinal);

            if (ChangeLoggerLevel(request, path))
            {
                response.StatusCode = (int)HttpStatusCode.OK;
                return;
            }

            response.StatusCode = (int)HttpStatusCode.BadRequest;
            return;
        }

        // GET request
        string serialInfo = HandleRequest(null);
        logger?.LogDebug("Returning: {info}", serialInfo);

        context.HandleContentNegotiation(logger);
        await context.Response.WriteAsync(serialInfo);
    }

    private bool ChangeLoggerLevel(HttpRequest request, string path)
    {
        var epPath = new PathString(path);

        if (request.Path.StartsWithSegments(epPath, out PathString remaining) && remaining.HasValue)
        {
            string loggerName = remaining.Value.TrimStart('/');

            Dictionary<string, string> change = ((LoggersEndpoint)Endpoint).DeserializeRequest(request.Body);

            change.TryGetValue("configuredLevel", out string level);

            logger?.LogDebug("Change Request: {name}, {level}", loggerName, level ?? "RESET");

            if (!string.IsNullOrEmpty(loggerName))
            {
                if (!string.IsNullOrEmpty(level) && LoggerLevels.MapLogLevel(level) == null)
                {
                    logger?.LogDebug("Invalid LogLevel specified: {level}", level);
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
