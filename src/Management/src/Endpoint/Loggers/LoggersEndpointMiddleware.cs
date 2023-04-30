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

internal sealed class LoggersEndpointMiddleware : EndpointMiddleware<Dictionary<string, object>, LoggersChangeRequest>
{
    public LoggersEndpointMiddleware(LoggersEndpoint endpoint, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        ILogger<LoggersEndpointMiddleware> logger)
        : base(endpoint, managementOptions, logger)
    {
    }

    public override Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        return Endpoint.Options.ShouldInvoke(ManagementOptions, context, Logger) ? HandleLoggersRequestAsync(context) : Task.CompletedTask;
    }

    internal async Task HandleLoggersRequestAsync(HttpContext context)
    {
        HttpRequest request = context.Request;
        HttpResponse response = context.Response;

        if (context.Request.Method == "POST")
        {
            // POST - change a logger level
            Logger.LogDebug("Incoming path: {path}", request.Path.Value);
            ManagementEndpointOptions mgmtOptions = ManagementOptions.GetFromContextPath(request.Path);

            string path = ManagementOptions == null
                ? Endpoint.Options.Path
                : $"{mgmtOptions.Path}/{Endpoint.Options.Path}".Replace("//", "/", StringComparison.Ordinal);

            if (await ChangeLoggerLevelAsync(context, path))
            {
                response.StatusCode = (int)HttpStatusCode.OK;
                return;
            }

            response.StatusCode = (int)HttpStatusCode.BadRequest;
            return;
        }

        // GET request
        string serialInfo = await HandleRequestAsync(null, context.RequestAborted);
        Logger.LogDebug("Returning: {info}", serialInfo);

        context.HandleContentNegotiation(Logger);
        await context.Response.WriteAsync(serialInfo);
    }

    private async Task<bool> ChangeLoggerLevelAsync(HttpContext context, string path)
    {
        var epPath = new PathString(path);
        HttpRequest request = context.Request;

        if (request.Path.StartsWithSegments(epPath, out PathString remaining) && remaining.HasValue)
        {
            string loggerName = remaining.Value.TrimStart('/');

            Dictionary<string, string> change = ((LoggersEndpoint)Endpoint).DeserializeRequest(request.Body);

            change.TryGetValue("configuredLevel", out string level);

            Logger.LogDebug("Change Request: {name}, {level}", loggerName, level ?? "RESET");

            if (!string.IsNullOrEmpty(loggerName))
            {
                if (!string.IsNullOrEmpty(level) && LoggerLevels.MapLogLevel(level) == null)
                {
                    Logger.LogDebug("Invalid LogLevel specified: {level}", level);
                }
                else
                {
                    var changeReq = new LoggersChangeRequest(loggerName, level);
                    await HandleRequestAsync(changeReq, context.RequestAborted);
                    return true;
                }
            }
        }

        return false;
    }
}
