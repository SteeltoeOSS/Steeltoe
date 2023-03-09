using System.Xml.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint;
internal class ActuatorEndpointMapper
{
    private readonly IEnumerable<IContextName> _contextNames;
    private readonly IOptionsMonitor<ManagementEndpointOptions> _managementOptions;
    private readonly IEnumerable<IEndpointMiddleware> _middlewares;
    private readonly ILogger<ActuatorEndpointMapper> _logger;

    public ActuatorEndpointMapper(
        IEnumerable<IContextName> contextNames,
        IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        IEnumerable<IEndpointMiddleware> middlewares,
        ILogger<ActuatorEndpointMapper> logger)
    {
        _contextNames = contextNames;
        _managementOptions = managementOptions;
        _middlewares = middlewares;
        _logger = logger;
    }
    public IEndpointConventionBuilder Map(IEndpointRouteBuilder endpointRouteBuilder, ActuatorConventionBuilder conventionBuilder = null)
    {
        var collection = new HashSet<string>();
        var contexts = new List<ManagementEndpointOptions>();
        conventionBuilder ??= new ActuatorConventionBuilder();

        foreach (var name in _contextNames)
        {
            var mgmtOption = _managementOptions.Get(name.Name);
            var path = mgmtOption.Path;

     
            foreach (var middleware in _middlewares)
            {
                if (name is ActuatorContext  && middleware.GetType() == typeof(CloudFoundryEndpointMiddleware)
                     || name is CFContext && middleware.GetType() == typeof(ActuatorHypermediaEndpointMiddleware))
                {
                    continue;
                }
                var middlewareType = middleware.GetType();
                RequestDelegate pipeline = endpointRouteBuilder.CreateApplicationBuilder().UseMiddleware(middlewareType).Build();
                var epPath = middleware.EndpointOptions.GetContextPath(mgmtOption);
                if (collection.Contains(epPath))
                {
                    _logger.LogError("Skipping over duplicate path at" + epPath);
                }
                else
                {
                    collection.Add(epPath);
                    IEndpointConventionBuilder builder = endpointRouteBuilder.MapMethods(epPath, middleware.EndpointOptions.AllowedVerbs, pipeline);
                    conventionBuilder.Add(builder); 
                }

            }

        }
        return (IEndpointConventionBuilder)conventionBuilder;
    }
}
