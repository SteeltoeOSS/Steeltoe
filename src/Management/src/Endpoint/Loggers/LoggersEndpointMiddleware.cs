// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Loggers;

public class LoggersEndpointMiddleware : EndpointMiddleware<Dictionary<string, object>, LoggersChangeRequest>, IMiddleware
{
    public LoggersEndpointMiddleware(/*RequestDelegate next,*/ LoggersEndpoint endpoint, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        ILogger<LoggersEndpointMiddleware> logger = null)
        : base(endpoint, managementOptions, logger)
    {
    }
    
    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (Endpoint.Options.ShouldInvoke(managementOptions, context, logger))
        {
            return HandleLoggersRequestAsync(context);
        }

        return Task.CompletedTask;
    }

    protected internal async Task HandleLoggersRequestAsync(HttpContext context)
    {
        HttpRequest request = context.Request;
        HttpResponse response = context.Response;

        var currentContext = managementOptions.GetCurrentContext(context);

        if (context.Request.Method == "POST")
        {
            // POST - change a logger level
            var paths = new List<string>();
            logger?.LogDebug("Incoming path: {path}", request.Path.Value);
            paths.Add(managementOptions == null ? Endpoint.Options.Path : $"{managementOptions.CurrentValue.Path}/{Endpoint.Options.Path}".Replace("//", "/", StringComparison.Ordinal)); //TODO: only one path here??!!

            foreach (string path in paths.Distinct())
            {
                if (ChangeLoggerLevel(request, path, currentContext.SerializerOptions))
                {
                    response.StatusCode = (int)HttpStatusCode.OK;
                    return;
                }
            }

            response.StatusCode = (int)HttpStatusCode.BadRequest;
            return;
        }

        // GET request
        string serialInfo = HandleRequest(null, currentContext.SerializerOptions);
        logger?.LogDebug("Returning: {info}", serialInfo);

        context.HandleContentNegotiation(logger);
        await context.Response.WriteAsync(serialInfo);
    }

    private bool ChangeLoggerLevel(HttpRequest request, string path, JsonSerializerOptions serializerOptions)
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
                    HandleRequest(changeReq, serializerOptions);
                    return true;
                }
            }
        }

        return false;
    }
}
