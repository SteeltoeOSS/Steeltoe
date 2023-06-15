// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Loggers;

internal sealed class LoggersEndpointMiddleware : EndpointMiddleware<ILoggersRequest, Dictionary<string, object>>
{
    private readonly IEnumerable<IContextName> _contextNames;

    public LoggersEndpointMiddleware(ILoggersEndpointHandler endpointHandler, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
         IEnumerable<IContextName> contextNames, ILogger<LoggersEndpointMiddleware> logger)
        : base(endpointHandler, managementOptions, logger)
    {
        _contextNames = contextNames;
    }

    protected override async Task<Dictionary<string, object>> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        ILoggersRequest loggersRequest = await GetLoggersChangeRequestAsync(context);

        if (loggersRequest is ErrorLoggersRequest)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            return null;
        }

        return await EndpointHandler.InvokeAsync(loggersRequest, cancellationToken);
    }

    private async Task<ILoggersRequest> GetLoggersChangeRequestAsync(HttpContext context)
    {
        HttpRequest request = context.Request;

        if (context.Request.Method == "POST")
        {
            // POST - change a logger level
            Logger.LogDebug("Incoming path: {path}", request.Path.Value);

            foreach (IContextName contextName in _contextNames)
            {
                ManagementEndpointOptions mgmtOption = ManagementEndpointOptions.Get(contextName.Name);
                string path = EndpointOptions.Path;

                if (mgmtOption.Path != null)
                {
                    path = mgmtOption.Path + "/" + path;
                    path = path.Replace("//", "/", StringComparison.Ordinal);
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

                        return new LoggersChangeRequest(loggerName, level);
                    }
                }
            }
        }

        return new DefaultLoggersRequest();
    }
}
