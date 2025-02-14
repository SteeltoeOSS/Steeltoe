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

public abstract class EndpointMiddleware<TRequest, TResponse> : IEndpointMiddleware
{
    private readonly ILogger _logger;
    protected IOptionsMonitor<ManagementOptions> ManagementOptionsMonitor { get; }
    protected IEndpointHandler<TRequest, TResponse> EndpointHandler { get; }

    public EndpointOptions EndpointOptions => EndpointHandler.Options;

    private protected virtual string ContentType => "application/vnd.spring-boot.actuator.v3+json";

    protected EndpointMiddleware(IEndpointHandler<TRequest, TResponse> endpointHandler, IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(endpointHandler);
        ArgumentNullException.ThrowIfNull(managementOptionsMonitor);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        EndpointHandler = endpointHandler;
        ManagementOptionsMonitor = managementOptionsMonitor;
        _logger = loggerFactory.CreateLogger<EndpointMiddleware<TRequest, TResponse>>();
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
                    _logger.LogTrace("{Method} method is unavailable at path {Path}.", context.Request.Method, context.Request.Path.Value);
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
                    _logger.LogDebug("Reading {Method} request at path {Path} using {MiddlewareType}.", context.Request.Method, context.Request.Path.Value,
                        GetType());

                    TRequest? request = await ParseRequestAsync(context, context.RequestAborted);
                    TResponse response = await InvokeEndpointHandlerAsync(request, context.RequestAborted);
                    await WriteResponseAsync(response, context, context.RequestAborted);
                }

                return;
            }
        }
        else
        {
            _logger.LogTrace("CanInvoke returned false for {Method} request at path {Path}.", context.Request.Method, context.Request.Path.Value);
        }

        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
    }

    private bool IsValidContentType(HttpRequest httpRequest)
    {
        if (httpRequest.ContentType == null)
        {
            return true;
        }

        // Media types are case-insensitive, according to https://stackoverflow.com/a/9842589.
        return MediaTypeHeaderValue.TryParse(httpRequest.ContentType, out MediaTypeHeaderValue? headerValue) && headerValue.MatchesMediaType(ContentType);
    }

    private bool IsCompatibleAcceptHeader(HttpRequest httpRequest)
    {
        string[] acceptHeaderValues = httpRequest.Headers.GetCommaSeparatedValues("Accept");

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

    protected virtual Task<TRequest?> ParseRequestAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        return Task.FromResult<TRequest?>(default);
    }

    protected abstract Task<TResponse> InvokeEndpointHandlerAsync(TRequest? request, CancellationToken cancellationToken);

    protected virtual async Task WriteResponseAsync(TResponse response, HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (Equals(response, null))
        {
            return;
        }

        httpContext.Response.Headers.Append("Content-Type", ContentType);

        JsonSerializerOptions options = ManagementOptionsMonitor.CurrentValue.SerializerOptions;
        await JsonSerializer.SerializeAsync(httpContext.Response.Body, response, options, cancellationToken);
    }
}
