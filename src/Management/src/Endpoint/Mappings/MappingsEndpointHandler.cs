// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Mappings;

internal sealed class MappingsEndpointHandler : IMappingsEndpointHandler
{
    private readonly IOptionsMonitor<MappingsEndpointOptions> _options;
    private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
    private readonly IEnumerable<IApiDescriptionProvider> _apiDescriptionProviders;
    private readonly IRouteMappings _routeMappings;
    private readonly ILogger<MappingsEndpointHandler> _logger;

    public HttpMiddlewareOptions Options => _options.CurrentValue;

    public MappingsEndpointHandler(IOptionsMonitor<MappingsEndpointOptions> options, ILoggerFactory loggerFactory, IRouteMappings routeMappings,
        IActionDescriptorCollectionProvider actionDescriptorCollectionProvider, IEnumerable<IApiDescriptionProvider> apiDescriptionProviders)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(loggerFactory);
        ArgumentGuard.NotNull(routeMappings);
        ArgumentGuard.NotNull(actionDescriptorCollectionProvider);
        ArgumentGuard.NotNull(apiDescriptionProviders);

        _options = options;
        _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
        _apiDescriptionProviders = apiDescriptionProviders;
        _routeMappings = routeMappings;
        _logger = loggerFactory.CreateLogger<MappingsEndpointHandler>();
    }

    public Task<ApplicationMappings> InvokeAsync(object arg, CancellationToken cancellationToken)
    {
        _logger.LogTrace("Fetching application mappings");
        return Task.Run(() => GetApplicationMappings(), cancellationToken);
    }

    internal ApplicationMappings GetApplicationMappings()
    {
        IDictionary<string, IList<MappingDescription>> desc = new Dictionary<string, IList<MappingDescription>>();

        if (_actionDescriptorCollectionProvider != null)
        {
            ApiDescriptionProviderContext apiContext = GetApiDescriptions(_actionDescriptorCollectionProvider?.ActionDescriptors?.Items);
            desc = GetMappingDescriptions(apiContext);
        }

        if (_routeMappings != null)
        {
            AddRouteMappingsDescriptions(_routeMappings, desc);
        }

        var contextMappings = new ContextMappings(desc);
        return new ApplicationMappings(contextMappings);
    }

    internal IDictionary<string, IList<MappingDescription>> GetMappingDescriptions(ApiDescriptionProviderContext apiContext)
    {
        IDictionary<string, IList<MappingDescription>> mappingDescriptions = new Dictionary<string, IList<MappingDescription>>();

        foreach (ApiDescription desc in apiContext.Results)
        {
            var descriptor = desc.ActionDescriptor as ControllerActionDescriptor;
            IRouteDetails details = GetRouteDetails(desc);
            mappingDescriptions.TryGetValue(descriptor.ControllerTypeInfo.FullName, out IList<MappingDescription> mapList);

            if (mapList == null)
            {
                mapList = new List<MappingDescription>();
                mappingDescriptions.Add(descriptor.ControllerTypeInfo.FullName, mapList);
            }

            var mapDesc = new MappingDescription(descriptor.MethodInfo, details);
            mapList.Add(mapDesc);
        }

        foreach (ActionDescriptor desc in apiContext.Actions)
        {
            if (desc is ControllerActionDescriptor descriptor)
            {
                if (apiContext.Results.Any() &&
                    mappingDescriptions.Any(description => description.Value.Any(n => n.Handler == descriptor.MethodInfo.ToString())))
                {
                    continue;
                }

                IRouteDetails details = GetRouteDetails(desc);
                mappingDescriptions.TryGetValue(descriptor.ControllerTypeInfo.FullName, out IList<MappingDescription> mapList);

                if (mapList == null)
                {
                    mapList = new List<MappingDescription>();
                    mappingDescriptions.Add(descriptor.ControllerTypeInfo.FullName, mapList);
                }

                var mapDesc = new MappingDescription(descriptor.MethodInfo, details);
                mapList.Add(mapDesc);
            }
        }

        return mappingDescriptions;
    }

    internal IRouteDetails GetRouteDetails(ApiDescription desc)
    {
        var routeDetails = new AspNetCoreRouteDetails
        {
            HttpMethods = GetHttpMethods(desc)
        };

        if (desc.ActionDescriptor.AttributeRouteInfo?.Template != null)
        {
            routeDetails.RouteTemplate = desc.ActionDescriptor.AttributeRouteInfo.Template;
        }
        else
        {
            var descriptor = desc.ActionDescriptor as ControllerActionDescriptor;
            routeDetails.RouteTemplate = $"/{descriptor.ControllerName}/{descriptor.ActionName}";
        }

        var produces = new List<string>();

        foreach (ApiResponseType respTypes in desc.SupportedResponseTypes)
        {
            foreach (ApiResponseFormat format in respTypes.ApiResponseFormats)
            {
                produces.Add(format.MediaType);
            }
        }

        routeDetails.Produces = produces;

        var consumes = new List<string>();

        foreach (ApiRequestFormat reqTypes in desc.SupportedRequestFormats)
        {
            consumes.Add(reqTypes.MediaType);
        }

        routeDetails.Consumes = consumes;

        return routeDetails;
    }

    internal IRouteDetails GetRouteDetails(ActionDescriptor desc)
    {
        var routeDetails = new AspNetCoreRouteDetails
        {
            HttpMethods = desc.ActionConstraints?.OfType<HttpMethodActionConstraint>().SingleOrDefault()?.HttpMethods.ToList() ?? new List<string>
            {
                MappingDescription.AllHttpMethods
            },
            Consumes = new List<string>(),
            Produces = new List<string>()
        };

        if (desc.AttributeRouteInfo?.Template != null)
        {
            routeDetails.RouteTemplate = desc.AttributeRouteInfo.Template;
        }
        else
        {
            var descriptor = desc as ControllerActionDescriptor;
            routeDetails.RouteTemplate = $"/{descriptor.ControllerName}/{descriptor.ActionName}";
        }

        foreach (ProducesAttribute filter in desc.FilterDescriptors.Where(f => f.Filter is ProducesAttribute).Select(f => (ProducesAttribute)f.Filter))
        {
            foreach (string format in filter.ContentTypes)
            {
                routeDetails.Produces.Add(format);
            }
        }

        foreach (ConsumesAttribute filter in desc.FilterDescriptors.Where(f => f.Filter is ConsumesAttribute).Select(f => (ConsumesAttribute)f.Filter))
        {
            foreach (string format in filter.ContentTypes)
            {
                routeDetails.Consumes.Add(format);
            }
        }

        return routeDetails;
    }

    internal IRouteDetails GetRouteDetails(Route route)
    {
        var routeDetails = new AspNetCoreRouteDetails
        {
            HttpMethods = GetHttpMethods(route),
            RouteTemplate = route.RouteTemplate
        };

        return routeDetails;
    }

    internal void AddRouteMappingsDescriptions(IRouteMappings routeMappings, IDictionary<string, IList<MappingDescription>> desc)
    {
        if (routeMappings == null)
        {
            return;
        }

        foreach (IRouter router in routeMappings.Routers)
        {
            if (router is Route route)
            {
                IRouteDetails details = GetRouteDetails(route);
                desc.TryGetValue("CoreRouteHandler", out IList<MappingDescription> mapList);

                if (mapList == null)
                {
                    mapList = new List<MappingDescription>();
                    desc.Add("CoreRouteHandler", mapList);
                }

                var mapDesc = new MappingDescription("CoreRouteHandler", details);
                mapList.Add(mapDesc);
            }
        }
    }

    private IList<string> GetHttpMethods(ApiDescription desc)
    {
        if (!string.IsNullOrEmpty(desc.HttpMethod))
        {
            return new List<string>
            {
                desc.HttpMethod
            };
        }

        return new List<string>();
    }

    private IList<string> GetHttpMethods(Route route)
    {
        IDictionary<string, IRouteConstraint> constraints = route.Constraints;

        if (constraints.TryGetValue("httpMethod", out IRouteConstraint routeConstraint) && routeConstraint is HttpMethodRouteConstraint methodConstraint)
        {
            return methodConstraint.AllowedMethods;
        }

        return new List<string>();
    }

    private ApiDescriptionProviderContext GetApiDescriptions(IReadOnlyList<ActionDescriptor> actionDescriptors)
    {
        if (actionDescriptors == null)
        {
            return new ApiDescriptionProviderContext(new List<ActionDescriptor>());
        }

        var context = new ApiDescriptionProviderContext(actionDescriptors);

        foreach (IApiDescriptionProvider provider in _apiDescriptionProviders)
        {
            provider.OnProvidersExecuting(context);
        }

        return context;
    }

   
}
