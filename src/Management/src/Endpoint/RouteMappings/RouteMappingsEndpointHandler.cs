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
    private readonly IOptionsMonitor<RouteMappingsEndpointOptions> _optionsMonitor;
    private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
    private readonly ICollection<IApiDescriptionProvider> _apiDescriptionProviders;
    private readonly RouterMappings _routerMappings;
    private readonly ILogger<RouteMappingsEndpointHandler> _logger;

    public EndpointOptions Options => _optionsMonitor.CurrentValue;

    public RouteMappingsEndpointHandler(IOptionsMonitor<RouteMappingsEndpointOptions> optionsMonitor,
        IActionDescriptorCollectionProvider actionDescriptorCollectionProvider, IEnumerable<IApiDescriptionProvider> apiDescriptionProviders,
        RouterMappings routerMappings, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(actionDescriptorCollectionProvider);
        ArgumentNullException.ThrowIfNull(apiDescriptionProviders);
        ArgumentNullException.ThrowIfNull(routerMappings);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        IApiDescriptionProvider[] apiDescriptionProviderArray = apiDescriptionProviders.ToArray();
        ArgumentGuard.ElementsNotNull(apiDescriptionProviderArray);

        _optionsMonitor = optionsMonitor;
        _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
        _apiDescriptionProviders = apiDescriptionProviderArray;
        _routerMappings = routerMappings;
        _logger = loggerFactory.CreateLogger<RouteMappingsEndpointHandler>();
    }

    public Task<RouteMappingsResponse> InvokeAsync(object? argument, CancellationToken cancellationToken)
    {
        _logger.LogTrace("Fetching application mappings");

        ApiDescriptionProviderContext apiContext = GetApiDescriptions(_actionDescriptorCollectionProvider.ActionDescriptors.Items);
        IDictionary<string, IList<RouteMappingDescription>> dictionary = GetMappingDescriptions(apiContext);

        AddRouteMappingsDescriptions(dictionary);

        var contextMappings = new ContextMappings(dictionary, null);
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
            mappingDescriptions.TryGetValue(descriptor.ControllerTypeInfo.FullName!, out IList<RouteMappingDescription>? descriptions);

            if (descriptions == null)
            {
                descriptions = new List<RouteMappingDescription>();
                mappingDescriptions.Add(descriptor.ControllerTypeInfo.FullName!, descriptions);
            }

            var routeMappingDescription = new RouteMappingDescription(descriptor.MethodInfo, details);
            descriptions.Add(routeMappingDescription);
        }

        foreach (ActionDescriptor descriptor in apiContext.Actions)
        {
            if (descriptor is ControllerActionDescriptor controllerDescriptor)
            {
                if (apiContext.Results.Any() && mappingDescriptions.Any(description =>
                    description.Value.Any(mappingDescription => mappingDescription.Handler == controllerDescriptor.MethodInfo.ToString())))
                {
                    continue;
                }

                AspNetCoreRouteDetails details = GetRouteDetails(descriptor);
                mappingDescriptions.TryGetValue(controllerDescriptor.ControllerTypeInfo.FullName!, out IList<RouteMappingDescription>? descriptions);

                if (descriptions == null)
                {
                    descriptions = new List<RouteMappingDescription>();
                    mappingDescriptions.Add(controllerDescriptor.ControllerTypeInfo.FullName!, descriptions);
                }

                var routeMappingDescription = new RouteMappingDescription(controllerDescriptor.MethodInfo, details);
                descriptions.Add(routeMappingDescription);
            }
        }

        return mappingDescriptions;
    }

    private AspNetCoreRouteDetails GetRouteDetails(ApiDescription description)
    {
        string routeTemplate;

        if (description.ActionDescriptor.AttributeRouteInfo?.Template != null)
        {
            routeTemplate = description.ActionDescriptor.AttributeRouteInfo.Template;
        }
        else
        {
            var descriptor = (ControllerActionDescriptor)description.ActionDescriptor;
            routeTemplate = $"/{descriptor.ControllerName}/{descriptor.ActionName}";
        }

        IList<string> httpMethods = GetHttpMethods(description);

        List<string> consumes = description.SupportedRequestFormats.Select(format => format.MediaType).ToList();

        if (description.ActionDescriptor.ActionConstraints != null)
        {
            foreach (ConsumesAttribute consumesAttribute in description.ActionDescriptor.ActionConstraints.OfType<ConsumesAttribute>())
            {
                consumes.AddRange(consumesAttribute.ContentTypes);
            }
        }

        var produces = new List<string>();

        foreach (ApiResponseType responseType in description.SupportedResponseTypes)
        {
            produces.AddRange(responseType.ApiResponseFormats.Select(format => format.MediaType));
        }

        return new AspNetCoreRouteDetails(routeTemplate, httpMethods, consumes, produces, new List<string>(), new List<string>());
    }

    private AspNetCoreRouteDetails GetRouteDetails(ActionDescriptor actionDescriptor)
    {
        string routeTemplate;

        if (actionDescriptor.AttributeRouteInfo?.Template != null)
        {
            routeTemplate = actionDescriptor.AttributeRouteInfo.Template;
        }
        else
        {
            var controllerDescriptor = (ControllerActionDescriptor)actionDescriptor;
            routeTemplate = $"/{controllerDescriptor.ControllerName}/{controllerDescriptor.ActionName}";
        }

        string[] httpMethods = actionDescriptor.ActionConstraints?.OfType<HttpMethodActionConstraint>().SingleOrDefault()?.HttpMethods.ToArray() ??
        [
            RouteMappingDescription.AllHttpMethods
        ];

        var consumes = new List<string>();

        foreach (ConsumesAttribute attribute in actionDescriptor.FilterDescriptors.Where(descriptor => descriptor.Filter is ConsumesAttribute)
            .Select(descriptor => (ConsumesAttribute)descriptor.Filter))
        {
            consumes.AddRange(attribute.ContentTypes);
        }

        var produces = new List<string>();

        foreach (ProducesAttribute attribute in actionDescriptor.FilterDescriptors.Where(descriptor => descriptor.Filter is ProducesAttribute)
            .Select(descriptor => (ProducesAttribute)descriptor.Filter))
        {
            produces.AddRange(attribute.ContentTypes);
        }

        return new AspNetCoreRouteDetails(routeTemplate, httpMethods, consumes, produces, new List<string>(), new List<string>());
    }

    private AspNetCoreRouteDetails GetRouteDetails(Route route)
    {
        string routeRouteTemplate = route.RouteTemplate ?? string.Empty;
        IList<string> httpMethods = GetHttpMethods(route);

        return new AspNetCoreRouteDetails(routeRouteTemplate, httpMethods, new List<string>(), new List<string>(), new List<string>(), new List<string>());
    }

    private void AddRouteMappingsDescriptions(IDictionary<string, IList<RouteMappingDescription>> dictionary)
    {
        foreach (IRouter router in _routerMappings.Routers)
        {
            if (router is Route route)
            {
                AspNetCoreRouteDetails details = GetRouteDetails(route);
                dictionary.TryGetValue("CoreRouteHandler", out IList<RouteMappingDescription>? descriptions);

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

        if (constraints.TryGetValue("httpMethod", out IRouteConstraint? routeConstraint) && routeConstraint is HttpMethodRouteConstraint methodConstraint)
        {
            return methodConstraint.AllowedMethods;
        }

        return new List<string>();
    }

    private ApiDescriptionProviderContext GetApiDescriptions(IReadOnlyList<ActionDescriptor> actionDescriptors)
    {
        var context = new ApiDescriptionProviderContext(actionDescriptors);

        foreach (IApiDescriptionProvider provider in _apiDescriptionProviders)
        {
            provider.OnProvidersExecuting(context);
        }

        return context;
    }
}
