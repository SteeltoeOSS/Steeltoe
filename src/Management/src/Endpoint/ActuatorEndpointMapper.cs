// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Web.Hypermedia;

namespace Steeltoe.Management.Endpoint;

internal sealed class ActuatorEndpointMapper
{
    private readonly IEnumerable<IContextName> _contextNames;
    private readonly IOptionsMonitor<ManagementEndpointOptions> _managementOptions;
    private readonly IEnumerable<IEndpointMiddleware> _middlewares;
    private readonly ILogger<ActuatorEndpointMapper> _logger;

    public ActuatorEndpointMapper(IEnumerable<IContextName> contextNames, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        IEnumerable<IEndpointMiddleware> middlewares, ILogger<ActuatorEndpointMapper> logger)
    {
        _contextNames = contextNames;
        _managementOptions = managementOptions;
        _middlewares = middlewares;
        _logger = logger;
    }

    public IEndpointConventionBuilder Map(IEndpointRouteBuilder endpointRouteBuilder, ref ActuatorConventionBuilder conventionBuilder)
    {
        var collection = new HashSet<string>();
        conventionBuilder ??= new ActuatorConventionBuilder();

        foreach (IContextName name in _contextNames)
        {
            ManagementEndpointOptions mgmtOption = _managementOptions.Get(name.Name);

            foreach (IEndpointMiddleware middleware in _middlewares)
            {
                if ((name is ActuatorContext && middleware is CloudFoundryEndpointMiddleware) ||
                    (name is CFContext && middleware is ActuatorHypermediaEndpointMiddleware))
                {
                    continue;
                }

                Type middlewareType = middleware.GetType();
                RequestDelegate pipeline = endpointRouteBuilder.CreateApplicationBuilder().UseMiddleware(middlewareType).Build();
                string epPath = middleware.EndpointOptions.CurrentValue.GetContextPath(mgmtOption);

                if (collection.Add(epPath))
                {
                    IEndpointConventionBuilder builder = endpointRouteBuilder.MapMethods(epPath, middleware.EndpointOptions.CurrentValue.AllowedVerbs, pipeline);
                    conventionBuilder.Add(builder);
                }
                else
                {
                    _logger.LogError("Skipping over duplicate path at {path}", epPath);
                }
            }
        }

        return conventionBuilder;
    }
}
