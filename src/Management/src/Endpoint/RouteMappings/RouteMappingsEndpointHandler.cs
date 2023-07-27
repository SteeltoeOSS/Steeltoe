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

namespace Steeltoe.Management.Endpoint.RouteMappings;

internal sealed class RouteMappingsEndpointHandler : IRouteMappingsEndpointHandler
{
    private readonly IOptionsMonitor<RouteMappingsEndpointOptions> _options;
    private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
    private readonly IEnumerable<IApiDescriptionProvider> _apiDescriptionProviders;
    private readonly RouteMappings _routeMappings;
    private readonly ILogger<RouteMappingsEndpointHandler> _logger;

    public HttpMiddlewareOptions Options => _options.CurrentValue;

    public RouteMappingsEndpointHandler(IOptionsMonitor<RouteMappingsEndpointOptions> options,
        IActionDescriptorCollectionProvider actionDescriptorCollectionProvider, IEnumerable<IApiDescriptionProvider> apiDescriptionProviders,
        RouteMappings routeMappings, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(actionDescriptorCollectionProvider);
        ArgumentGuard.NotNull(apiDescriptionProviders);
        ArgumentGuard.NotNull(routeMappings);
        ArgumentGuard.NotNull(loggerFactory);

        _options = options;
        _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
        _apiDescriptionProviders = apiDescriptionProviders;
        _routeMappings = routeMappings;
        _logger = loggerFactory.CreateLogger<RouteMappingsEndpointHandler>();
    }

    public Task<RouteMappingsResponse> InvokeAsync(object argument, CancellationToken cancellationToken)
    {
        _logger.LogTrace("Fetching application mappings");
        IDictionary<string, IList<RouteMappingDescription>> dictionary = new Dictionary<string, IList<RouteMappingDescription>>();

        if (_actionDescriptorCollectionProvider != null)
        {
            ApiDescriptionProviderContext apiContext = GetApiDescriptions(_actionDescriptorCollectionProvider?.ActionDescriptors.Items);
            dictionary = GetMappingDescriptions(apiContext);
        }

        if (_routeMappings != null)
        {
            AddRouteMappingsDescriptions(_routeMappings, dictionary);
        }

        var contextMappings = new ContextMappings(dictionary);
        var response = new RouteMappingsResponse(contextMappings);

        return Task.FromResult(response);
    }

    private IDictionary<string, IList<RouteMappingDescription>> GetMappingDescriptions(ApiDescriptionProviderContext apiContext)
    {
        IDictionary<string, IList<RouteMappingDescription>> mappingDescriptions = new Dictionary<string, IList<RouteMappingDescription>>();

        foreach (ApiDescription description in apiContext.Results)
        {
            var descriptor = (ControllerActionDescriptor)description.ActionDescriptor;
            AspNetCoreRouteDetails details = GetRouteDetails(description);
            mappingDescriptions.TryGetValue(descriptor.ControllerTypeInfo.FullName!, out IList<RouteMappingDescription> descriptions);

            if (descriptions == null)
            {
                descriptions = new List<RouteMappingDescription>();
                mappingDescriptions.Add(descriptor.ControllerTypeInfo.FullName, descriptions);
            }

            var routeMappingDescription = new RouteMappingDescription(descriptor.MethodInfo, details);
            descriptions.Add(routeMappingDescription);
        }

        foreach (ActionDescriptor descriptor in apiContext.Actions)
        {
            if (descriptor is ControllerActionDescriptor controllerDescriptor)
            {
                if (apiContext.Results.Any() &&
                    mappingDescriptions.Any(description => description.Value.Any(n => n.Handler == controllerDescriptor.MethodInfo.ToString())))
                {
                    continue;
                }

                AspNetCoreRouteDetails details = GetRouteDetails(descriptor);
                mappingDescriptions.TryGetValue(controllerDescriptor.ControllerTypeInfo.FullName!, out IList<RouteMappingDescription> descriptions);

                if (descriptions == null)
                {
                    descriptions = new List<RouteMappingDescription>();
                    mappingDescriptions.Add(controllerDescriptor.ControllerTypeInfo.FullName, descriptions);
                }

                var routeMappingDescription = new RouteMappingDescription(controllerDescriptor.MethodInfo, details);
                descriptions.Add(routeMappingDescription);
            }
        }

        return mappingDescriptions;
    }

    private AspNetCoreRouteDetails GetRouteDetails(ApiDescription description)
    {
        var routeDetails = new AspNetCoreRouteDetails
        {
            HttpMethods = GetHttpMethods(description)
        };

        if (description.ActionDescriptor.AttributeRouteInfo?.Template != null)
        {
            routeDetails.RouteTemplate = description.ActionDescriptor.AttributeRouteInfo.Template;
        }
        else
        {
            var descriptor = (ControllerActionDescriptor)description.ActionDescriptor;
            routeDetails.RouteTemplate = $"/{descriptor.ControllerName}/{descriptor.ActionName}";
        }

        var produces = new List<string>();

        foreach (ApiResponseType responseType in description.SupportedResponseTypes)
        {
            foreach (ApiResponseFormat format in responseType.ApiResponseFormats)
            {
                produces.Add(format.MediaType);
            }
        }

        routeDetails.Produces = produces;

        var consumes = new List<string>();

        foreach (ApiRequestFormat format in description.SupportedRequestFormats)
        {
            consumes.Add(format.MediaType);
        }

        routeDetails.Consumes = consumes;

        return routeDetails;
    }

    private AspNetCoreRouteDetails GetRouteDetails(ActionDescriptor descriptor)
    {
        var routeDetails = new AspNetCoreRouteDetails
        {
            HttpMethods = descriptor.ActionConstraints?.OfType<HttpMethodActionConstraint>().SingleOrDefault()?.HttpMethods.ToList() ?? new List<string>
            {
                RouteMappingDescription.AllHttpMethods
            },
            Consumes = new List<string>(),
            Produces = new List<string>()
        };

        if (descriptor.AttributeRouteInfo?.Template != null)
        {
            routeDetails.RouteTemplate = descriptor.AttributeRouteInfo.Template;
        }
        else
        {
            var controllerDescriptor = (ControllerActionDescriptor)descriptor;
            routeDetails.RouteTemplate = $"/{controllerDescriptor.ControllerName}/{controllerDescriptor.ActionName}";
        }

        foreach (ProducesAttribute filter in descriptor.FilterDescriptors.Where(f => f.Filter is ProducesAttribute).Select(f => (ProducesAttribute)f.Filter))
        {
            foreach (string format in filter.ContentTypes)
            {
                routeDetails.Produces.Add(format);
            }
        }

        foreach (ConsumesAttribute filter in descriptor.FilterDescriptors.Where(f => f.Filter is ConsumesAttribute).Select(f => (ConsumesAttribute)f.Filter))
        {
            foreach (string format in filter.ContentTypes)
            {
                routeDetails.Consumes.Add(format);
            }
        }

        return routeDetails;
    }

    private AspNetCoreRouteDetails GetRouteDetails(Route route)
    {
        var routeDetails = new AspNetCoreRouteDetails
        {
            HttpMethods = GetHttpMethods(route),
            RouteTemplate = route.RouteTemplate
        };

        return routeDetails;
    }

    private void AddRouteMappingsDescriptions(RouteMappings routeMappings, IDictionary<string, IList<RouteMappingDescription>> dictionary)
    {
        if (routeMappings == null)
        {
            return;
        }

        foreach (IRouter router in routeMappings.Routers)
        {
            if (router is Route route)
            {
                AspNetCoreRouteDetails details = GetRouteDetails(route);
                dictionary.TryGetValue("CoreRouteHandler", out IList<RouteMappingDescription> descriptions);

                if (descriptions == null)
                {
                    descriptions = new List<RouteMappingDescription>();
                    dictionary.Add("CoreRouteHandler", descriptions);
                }

                var routeMappingDescription = new RouteMappingDescription("CoreRouteHandler", details);
                descriptions.Add(routeMappingDescription);
            }
        }
    }

    private IList<string> GetHttpMethods(ApiDescription description)
    {
        if (!string.IsNullOrEmpty(description.HttpMethod))
        {
            return new List<string>
            {
                description.HttpMethod
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
