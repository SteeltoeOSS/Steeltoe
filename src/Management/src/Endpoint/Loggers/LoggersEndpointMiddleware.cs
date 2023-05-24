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

internal sealed class LoggersEndpointMiddleware : EndpointMiddleware<ILoggersRequest, Dictionary<string, object>>
{
    private readonly IEnumerable<IContextName> _contextNames;

    public LoggersEndpointMiddleware(ILoggersEndpointHandler endpointHandler,
        IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        IOptionsMonitor<HttpMiddlewareOptions> endpointOptions,
        IEnumerable<IContextName> contextNames,
        ILogger<LoggersEndpointMiddleware> logger)
        : base(endpointHandler, managementOptions,  logger)
    {
        _contextNames = contextNames;
    }

    protected override async Task<Dictionary<string, object>> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var loggersRequest = await GetLoggersChangeRequestAsync(context);
        if(loggersRequest is ErrorLoggersRequest)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }
        return await  EndpointHandler.InvokeAsync(loggersRequest, cancellationToken);
    }

    //public override Task InvokeAsync(HttpContext context, RequestDelegate next)
    //{
    //    return EndpointOptions.CurrentValue.ShouldInvoke(ManagementOptions, context, Logger) ? HandleLoggersRequestAsync(context) : Task.CompletedTask;
    //}

    //internal async Task HandleLoggersRequestAsync(HttpContext context)
    //{
    //    HttpRequest request = context.Request;
    //    HttpResponse response = context.Response;

    //    if (context.Request.Method == "POST")
    //    {
    //        // POST - change a logger level
    //        Logger.LogDebug("Incoming path: {path}", request.Path.Value);
    //        ManagementEndpointOptions mgmtOptions = ManagementOptions.GetFromContextPath(request.Path);

    //        string path = ManagementOptions == null
    //            ? EndpointOptions.CurrentValue.Path
    //            : $"{mgmtOptions.Path}/{EndpointOptions.CurrentValue.Path}".Replace("//", "/", StringComparison.Ordinal);

    //        if (await ChangeLoggerLevelAsync(context, path))
    //        {
    //            response.StatusCode = (int)HttpStatusCode.OK;
    //            return;
    //        }

    //        response.StatusCode = (int)HttpStatusCode.BadRequest;
    //        return;
    //    }

    //    // GET request
    //    string serialInfo = await HandleRequestAsync(null, context.RequestAborted);
    //    Logger.LogDebug("Returning: {info}", serialInfo);

    //    context.HandleContentNegotiation(Logger);
    //    await context.Response.WriteAsync(serialInfo);
    //}
    private async Task<ILoggersRequest> GetLoggersChangeRequestAsync(HttpContext context)
    {
        HttpRequest request = context.Request;

        if (context.Request.Method == "POST")
        {
            // POST - change a logger level
            Logger.LogDebug("Incoming path: {path}", request.Path.Value);


            //var optionsPath =
            foreach (var contextName in _contextNames)
            {
                var mgmtOption = ManagementEndpointOptions.Get(contextName.Name);
                string path = EndpointOptions.Path;

                if (mgmtOption.Path != null)
                {
                    path = mgmtOption.Path + "/" + path;
                }
                var epPath = new PathString(path);

                if (request.Path.StartsWithSegments(epPath, out PathString remaining) && remaining.HasValue)
                {
                    string loggerName = remaining.Value.TrimStart('/');

                    Dictionary<string, string> change = await ((LoggersEndpointHandler)EndpointHandler).DeserializeRequestAsync(request.Body);

                    change.TryGetValue("configuredLevel", out string level);

                    Logger.LogDebug("Change Request: {name}, {level}", loggerName, level ?? "RESET");

                    if (!string.IsNullOrEmpty(loggerName))
                    {
                        if (!string.IsNullOrEmpty(level) && LoggerLevels.MapLogLevel(level) == null)
                        {
                            Logger.LogDebug("Invalid LogLevel specified: {level}", level);
                            return new ErrorLoggersRequest();
                        }
                        else
                        {
                            return new LoggersChangeRequest(loggerName, level);
                        }
                    }
                }
            }
        
        }
        return new DefaultLoggersRequest();
    }
    //private async Task<bool> ChangeLoggerLevelAsync(HttpContext context, string configuredPath)
    //{
    //    var epPath = new PathString(configuredPath);
    //    HttpRequest request = context.Request;

    //    if (request.Path.StartsWithSegments(epPath, out PathString remaining) && remaining.HasValue)
    //    {
    //        string loggerName = remaining.Value.TrimStart('/');

    //        Dictionary<string, string> change = await ((LoggersEndpointHandler)EndpointHandler).DeserializeRequestAsync(request.Body);

    //        change.TryGetValue("configuredLevel", out string level);

    //        Logger.LogDebug("Change Request: {name}, {level}", loggerName, level ?? "RESET");

    //        if (!string.IsNullOrEmpty(loggerName))
    //        {
    //            if (!string.IsNullOrEmpty(level) && LoggerLevels.MapLogLevel(level) == null)
    //            {
    //                Logger.LogDebug("Invalid LogLevel specified: {level}", level);
    //            }
    //            else
    //            {
    //                var changeReq = new LoggersChangeRequest(loggerName, level);
    //                await HandleRequestAsync(changeReq, context.RequestAborted);
    //                return true;
    //            }
    //        }
    //    }

    //    return false;
    //}
}
