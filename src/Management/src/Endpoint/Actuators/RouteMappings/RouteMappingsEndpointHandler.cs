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
using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappings;

internal sealed class RouteMappingsEndpointHandler : IRouteMappingsEndpointHandler
{
    private readonly IOptionsMonitor<RouteMappingsEndpointOptions> _optionsMonitor;
    private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
    private readonly IApiDescriptionProvider[] _apiDescriptionProviders;
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
        Dictionary<string, IList<RouteMappingDescription>> dictionary = GetMappingDescriptions(apiContext);

        AddRouteMappingsDescriptions(dictionary);

        var contextMappings = new ContextMappings(dictionary, null);
        var response = new RouteMappingsResponse(contextMappings);

        return Task.FromResult(response);
    }

    private Dictionary<string, IList<RouteMappingDescription>> GetMappingDescriptions(ApiDescriptionProviderContext apiContext)
    {
        Dictionary<string, IList<RouteMappingDescription>> mappingDescriptions = [];

        foreach (ApiDescription description in apiContext.Results)
        {
            if (description.ActionDescriptor is ControllerActionDescriptor descriptor)
            {
                string controllerTypeName = descriptor.ControllerTypeInfo.FullName!;
                AspNetCoreRouteDetails details = GetRouteDetails(description);
                mappingDescriptions.TryGetValue(controllerTypeName, out IList<RouteMappingDescription>? descriptions);

                if (descriptions == null)
                {
                    descriptions = new List<RouteMappingDescription>();
                    mappingDescriptions.Add(controllerTypeName, descriptions);
                }

                var routeMappingDescription = new RouteMappingDescription(descriptor.MethodInfo, details);
                descriptions.Add(routeMappingDescription);
            }
            else
            {
                const string controllerTypeName = "UnknownController";
                AspNetCoreRouteDetails details = GetRouteDetails(description);
                mappingDescriptions.TryGetValue(controllerTypeName, out IList<RouteMappingDescription>? descriptions);

                if (descriptions == null)
                {
                    descriptions = new List<RouteMappingDescription>();
                    mappingDescriptions.Add(controllerTypeName, descriptions);
                }

                var routeMappingDescription = new RouteMappingDescription(description.ActionDescriptor.DisplayName ?? description.ActionDescriptor.Id, details);
                descriptions.Add(routeMappingDescription);
            }
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
                string controllerTypeName = controllerDescriptor.ControllerTypeInfo.FullName!;
                mappingDescriptions.TryGetValue(controllerTypeName, out IList<RouteMappingDescription>? descriptions);

                if (descriptions == null)
                {
                    descriptions = new List<RouteMappingDescription>();
                    mappingDescriptions.Add(controllerTypeName, descriptions);
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
            if (description.ActionDescriptor is ControllerActionDescriptor descriptor)
            {
                routeTemplate = $"/{descriptor.ControllerName}/{descriptor.ActionName}";
            }
            else
            {
                routeTemplate = description.RelativePath ?? description.ToString()!;
            }
        }

        List<string> httpMethods = GetHttpMethods(description);

        List<string> consumes = description.SupportedRequestFormats.Select(format => format.MediaType).ToList();

        if (description.ActionDescriptor.ActionConstraints != null)
        {
            foreach (ConsumesAttribute consumesAttribute in description.ActionDescriptor.ActionConstraints.OfType<ConsumesAttribute>())
            {
                consumes.AddRange(consumesAttribute.ContentTypes);
            }
        }

        List<string> produces = [];

        foreach (ApiResponseType responseType in description.SupportedResponseTypes)
        {
            produces.AddRange(responseType.ApiResponseFormats.Select(format => format.MediaType));
        }

        return new AspNetCoreRouteDetails(routeTemplate, httpMethods, consumes, produces, Array.Empty<string>(), Array.Empty<string>());
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

        List<string> consumes = [];

        foreach (ConsumesAttribute attribute in actionDescriptor.FilterDescriptors.Where(descriptor => descriptor.Filter is ConsumesAttribute)
            .Select(descriptor => (ConsumesAttribute)descriptor.Filter))
        {
            consumes.AddRange(attribute.ContentTypes);
        }

        List<string> produces = [];

        foreach (ProducesAttribute attribute in actionDescriptor.FilterDescriptors.Where(descriptor => descriptor.Filter is ProducesAttribute)
            .Select(descriptor => (ProducesAttribute)descriptor.Filter))
        {
            produces.AddRange(attribute.ContentTypes);
        }

        return new AspNetCoreRouteDetails(routeTemplate, httpMethods, consumes, produces, Array.Empty<string>(), Array.Empty<string>());
    }

    private AspNetCoreRouteDetails GetRouteDetails(Route route)
    {
        string routeRouteTemplate = route.RouteTemplate ?? string.Empty;
        IList<string> httpMethods = GetHttpMethods(route);

        return new AspNetCoreRouteDetails(routeRouteTemplate, httpMethods, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(),
            Array.Empty<string>());
    }

    private void AddRouteMappingsDescriptions(Dictionary<string, IList<RouteMappingDescription>> dictionary)
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

    private List<string> GetHttpMethods(ApiDescription description)
    {
        return !string.IsNullOrEmpty(description.HttpMethod) ? [description.HttpMethod] : [];
    }

    private IList<string> GetHttpMethods(Route route)
    {
        IDictionary<string, IRouteConstraint> constraints = route.Constraints;

        if (constraints.TryGetValue("httpMethod", out IRouteConstraint? routeConstraint) && routeConstraint is HttpMethodRouteConstraint methodConstraint)
        {
            return methodConstraint.AllowedMethods;
        }

        return Array.Empty<string>();
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
