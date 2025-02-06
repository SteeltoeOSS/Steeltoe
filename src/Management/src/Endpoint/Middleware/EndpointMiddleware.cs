// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Middleware;

public abstract class EndpointMiddleware<TArgument, TResult> : IEndpointMiddleware
{
    private readonly ILogger _logger;
    protected IOptionsMonitor<ManagementOptions> ManagementOptionsMonitor { get; }
    protected IEndpointHandler<TArgument, TResult> EndpointHandler { get; }

    public EndpointOptions EndpointOptions => EndpointHandler.Options;

    private protected virtual string ContentType => "application/vnd.spring-boot.actuator.v3+json";

    protected EndpointMiddleware(IEndpointHandler<TArgument, TResult> endpointHandler, IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(endpointHandler);
        ArgumentNullException.ThrowIfNull(managementOptionsMonitor);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        EndpointHandler = endpointHandler;
        ManagementOptionsMonitor = managementOptionsMonitor;
        _logger = loggerFactory.CreateLogger<EndpointMiddleware<TArgument, TResult>>();
    }

    public virtual ActuatorMetadataProvider GetMetadataProvider()
    {
        return new ActuatorMetadataProvider(ContentType);
    }

    public virtual bool CanInvoke(PathString requestPath)
    {
        return EndpointOptions.CanInvoke(requestPath, ManagementOptionsMonitor.CurrentValue);
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate? next)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (CanInvoke(context.Request.Path))
        {
            HashSet<string> allowedVerbs = EndpointOptions.GetSafeAllowedVerbs();

            if (allowedVerbs.Count > 0)
            {
                if (!allowedVerbs.Contains(context.Request.Method))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                }
                else if (!IsValidContentType(context.Request))
                {
                    _logger.LogDebug("Content-Type header '{RequestContentType}' is not supported for this request.", context.Request.ContentType);
                    context.Response.StatusCode = (int)HttpStatusCode.UnsupportedMediaType;
                    await context.Response.WriteAsync($"Only the '{ContentType}' content type is supported.");
                }
                else if (!IsCompatibleAcceptHeader(context.Request))
                {
                    _logger.LogDebug("Accept header '{AcceptType}' is not supported for this request.", context.Request.Headers.Accept.ToString());
                    context.Response.StatusCode = (int)HttpStatusCode.NotAcceptable;
                    await context.Response.WriteAsync($"Only the '{ContentType}' content type is supported.");
                }
                else
                {
                    TResult result = await InvokeEndpointHandlerAsync(context, context.RequestAborted);
                    await WriteResponseAsync(result, context, context.RequestAborted);
                }

                return;
            }
        }

        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
    }

    private bool IsValidContentType(HttpRequest request)
    {
        if (request.ContentType == null)
        {
            return true;
        }

        // Media types are case-insensitive, according to https://stackoverflow.com/a/9842589.
        return MediaTypeHeaderValue.TryParse(request.ContentType, out MediaTypeHeaderValue? headerValue) && headerValue.MatchesMediaType(ContentType);
    }

    private bool IsCompatibleAcceptHeader(HttpRequest request)
    {
        string[] acceptHeaderValues = request.Headers.GetCommaSeparatedValues("Accept");

        if (acceptHeaderValues.Length == 0)
        {
            return true;
        }

        foreach (string acceptHeaderValue in acceptHeaderValues)
        {
            if (MediaTypeHeaderValue.TryParse(acceptHeaderValue, out MediaTypeHeaderValue? headerValue) && headerValue.MatchesMediaType(ContentType))
            {
                return true;
            }
        }

        return false;
    }

    protected abstract Task<TResult> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken);

    protected virtual async Task WriteResponseAsync(TResult result, HttpContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (Equals(result, null))
        {
            return;
        }

        context.Response.Headers.Append("Content-Type", ContentType);

        JsonSerializerOptions options = ManagementOptionsMonitor.CurrentValue.SerializerOptions;
        await JsonSerializer.SerializeAsync(context.Response.Body, result, options, cancellationToken);
    }
}
