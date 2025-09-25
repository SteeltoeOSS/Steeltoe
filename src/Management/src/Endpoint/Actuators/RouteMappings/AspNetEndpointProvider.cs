// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings.RoutingTypes;
using Steeltoe.Management.Endpoint.Middleware;
using MicrosoftEndpoint = Microsoft.AspNetCore.Http.Endpoint;

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappings;

/// <summary>
/// Gathers endpoints in an ASP.NET Core application by combining information from various sources.
/// </summary>
internal sealed class AspNetEndpointProvider
{
    private static readonly MethodInfo? ProducesContentTypesPropertyGetter =
        typeof(ProducesResponseTypeAttribute).GetProperty("ContentTypes", BindingFlags.Instance | BindingFlags.NonPublic)?.GetMethod;

    private readonly IApiDescriptionGroupCollectionProvider _apiDescriptionGroupCollectionProvider;
    private readonly IEnumerable<EndpointDataSource> _endpointDataSources;
    private readonly IOptionsMonitor<MvcOptions> _mvcOptionsMonitor;
    private readonly ActuatorRouteOptionsResolver _actuatorRouteOptionsResolver;
    private readonly ILogger<AspNetEndpointProvider> _logger;

    public AspNetEndpointProvider(IApiDescriptionGroupCollectionProvider apiDescriptionGroupCollectionProvider,
        IEnumerable<EndpointDataSource> endpointDataSources, IOptionsMonitor<MvcOptions> mvcOptionsMonitor,
        ActuatorRouteOptionsResolver actuatorRouteOptionsResolver, ILogger<AspNetEndpointProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(apiDescriptionGroupCollectionProvider);
        ArgumentNullException.ThrowIfNull(endpointDataSources);
        ArgumentNullException.ThrowIfNull(mvcOptionsMonitor);
        ArgumentNullException.ThrowIfNull(actuatorRouteOptionsResolver);
        ArgumentNullException.ThrowIfNull(logger);

        _apiDescriptionGroupCollectionProvider = apiDescriptionGroupCollectionProvider;
        _endpointDataSources = endpointDataSources;
        _mvcOptionsMonitor = mvcOptionsMonitor;
        _actuatorRouteOptionsResolver = actuatorRouteOptionsResolver;
        _logger = logger;
    }

    public IList<AspNetEndpoint> GetEndpoints(bool includeActuators)
    {
        if (!_mvcOptionsMonitor.CurrentValue.EnableEndpointRouting)
        {
            _logger.LogWarning("Conventional routing is not supported.");
            return [];
        }

        ApiDescription[] apiDescriptions = _apiDescriptionGroupCollectionProvider.ApiDescriptionGroups.Items.SelectMany(group => group.Items).ToArray();
        IEnumerable<MicrosoftEndpoint> endpoints = FlattenDataSources(_endpointDataSources).SelectMany(source => source.Endpoints);
        ActuatorRoutesSnapshot? actuatorsSnapshot = null;

        List<AspNetEndpoint> aspNetEndpoints = [];

        foreach (MicrosoftEndpoint endpoint in endpoints)
        {
            if (endpoint.Metadata.GetMetadata<ActuatorMetadataProvider>() != null)
            {
                if (includeActuators)
                {
                    actuatorsSnapshot ??= _actuatorRouteOptionsResolver.CreateSnapshot();
                    aspNetEndpoints.AddRange(FromActuatorEndpoint((RouteEndpoint)endpoint, actuatorsSnapshot));
                }
            }
            else
            {
                // When available, API Explorer (for Minimal APIs and API Controllers) is the preferred model because it provides the richest metadata.
                // For example, it performs speculative model-binding to include parameters missing from the route pattern, while hiding injected parameters.
                // See https://github.com/dotnet/aspnetcore/blob/release/8.0/src/Mvc/Mvc.ApiExplorer/src/DefaultApiDescriptionProvider.cs#L166.

                List<ApiDescription> descriptionsForEndpoint = FindApiDescriptionsForEndpoint(endpoint, apiDescriptions);

                aspNetEndpoints.AddRange(descriptionsForEndpoint.Count > 0
                    ? descriptionsForEndpoint.Select(FromApiDescription)
                    : FromMicrosoftEndpoint(endpoint));
            }
        }

        return aspNetEndpoints;
    }

    private static IEnumerable<EndpointDataSource> FlattenDataSources(IEnumerable<EndpointDataSource> sources)
    {
        foreach (EndpointDataSource source in sources)
        {
            if (source is CompositeEndpointDataSource compositeSource)
            {
                foreach (EndpointDataSource innerSource in FlattenDataSources(compositeSource.DataSources))
                {
                    yield return innerSource;
                }
            }
            else if (source.GetType().FullName == "Microsoft.AspNetCore.StaticAssets.StaticAssetsEndpointDataSource")
            {
                // Excluded because it explodes the list of endpoints. Produced from "app.MapStaticAssets()", which is new in .NET 9.
            }
            else
            {
                yield return source;
            }
        }
    }

    private static List<ApiDescription> FindApiDescriptionsForEndpoint(MicrosoftEndpoint endpoint, IEnumerable<ApiDescription> descriptions)
    {
        // A MicrosoftEndpoint for multiple HTTP verbs gets expanded into individual ApiDescription entries. Try to match up.
        List<ApiDescription> candidates = [];

        MethodInfo? endpointHandlerMethod = GetHandlerMethod(endpoint);

        if (endpointHandlerMethod != null)
        {
            HashSet<string> endpointVerbs =
                AspNetEndpoint.ExplodeHttpMethods(endpoint.Metadata.OfType<IHttpMethodMetadata>().SelectMany(meta => meta.HttpMethods).ToArray());

            foreach (ApiDescription description in descriptions)
            {
                MethodInfo? apiHandlerMethod = GetHandlerMethod(description);
                string? apiVerb = description.HttpMethod?.ToUpperInvariant();

                if (ReferenceEquals(apiHandlerMethod, endpointHandlerMethod) && HaveSameRoute(endpoint, description) && apiVerb != null &&
                    endpointVerbs.Contains(apiVerb))
                {
                    candidates.Add(description);
                }
            }
        }

        return candidates;
    }

    private static bool HaveSameRoute(MicrosoftEndpoint endpoint, ApiDescription description)
    {
        if (endpoint is RouteEndpoint routeEndpoint)
        {
            string apiTemplate = GetRouteTemplate(description);
            string? endpointTemplate = routeEndpoint.RoutePattern.RawText;
            return apiTemplate == endpointTemplate;
        }

        return false;
    }

    private static IEnumerable<AspNetEndpoint> FromActuatorEndpoint(RouteEndpoint endpoint, ActuatorRoutesSnapshot snapshot)
    {
        string? routePattern = endpoint.RoutePattern.RawText;
        EndpointOptions? endpointOptions = snapshot.GetOptionsForRoute(routePattern);

        if (endpointOptions != null)
        {
            string displayName = endpoint.DisplayName ?? string.Empty;
            MethodInfo handlerMethod = typeof(EndpointMiddleware<,>).GetMethod(nameof(EndpointMiddleware<,>.InvokeAsync))!;
            var metadataProvider = endpoint.Metadata.GetMetadata<ActuatorMetadataProvider>()!;

            foreach (string httpMethod in endpointOptions.GetSafeAllowedVerbs())
            {
                EndpointMetadataCollection actuatorMetadata = metadataProvider.GetMetadata(httpMethod);

                List<AspNetEndpointParameter> parameters =
                    actuatorMetadata.GetOrderedMetadata<ApiParameterDescription>().Select(ToAspNetEndpointParameter).ToList();

                List<string> consumes = ExtractConsumedContentTypes(actuatorMetadata).ToList();
                List<string> produces = ExtractProducedContentTypes(actuatorMetadata).ToList();

                yield return new AspNetEndpoint(AspNetEndpointSource.ActuatorEndpointDataSource, displayName, routePattern, [httpMethod], handlerMethod,
                    parameters, consumes, produces);
            }
        }
    }

    private static AspNetEndpoint FromApiDescription(ApiDescription description)
    {
        var metadata = new EndpointMetadataCollection(description.ActionDescriptor.EndpointMetadata);
        string displayName = description.ActionDescriptor.DisplayName ?? string.Empty;
        string routePattern = GetRouteTemplate(description);
        List<string> httpMethods = description.HttpMethod == null ? [] : [description.HttpMethod];
        MethodInfo? handlerMethod = GetHandlerMethod(description);
        List<AspNetEndpointParameter> parameters = description.ParameterDescriptions.Select(ToAspNetEndpointParameter).ToList();
        List<string> consumes = ExtractConsumedContentTypes(metadata).ToList();

        List<string> produces = ExtractProducedContentTypes(metadata).Concat(description.SupportedResponseTypes.SelectMany(responseType =>
            responseType.ApiResponseFormats.Select(responseFormat => responseFormat.MediaType))).ToList();

        return new AspNetEndpoint(AspNetEndpointSource.ApiExplorer, displayName, routePattern, httpMethods, handlerMethod, parameters, consumes, produces);
    }

    private static IEnumerable<AspNetEndpoint> FromMicrosoftEndpoint(MicrosoftEndpoint endpoint)
    {
        var razorPagesDescriptor = endpoint.Metadata.GetMetadata<CompiledPageActionDescriptor>();

        if (razorPagesDescriptor != null)
        {
            // Expand the action methods in a Razor Pages model into separate endpoints.
            foreach (AspNetEndpoint aspNetEndpoint in FromRazorPagesEndpoint(endpoint, razorPagesDescriptor))
            {
                yield return aspNetEndpoint;
            }
        }
        else
        {
            if (endpoint.Metadata.GetMetadata<SuppressMatchingMetadata>() == null)
            {
                yield return FromMvcControllerEndpoint(endpoint);
            }
        }
    }

    private static IEnumerable<AspNetEndpoint> FromRazorPagesEndpoint(MicrosoftEndpoint endpoint, CompiledPageActionDescriptor pageDescriptor)
    {
        foreach (HandlerMethodDescriptor handlerDescriptor in pageDescriptor.HandlerMethods)
        {
            string displayName = endpoint.DisplayName ?? string.Empty;
            string? routePattern = null;
            List<string> httpMethods = [handlerDescriptor.HttpMethod];
            MethodInfo handlerMethod = handlerDescriptor.MethodInfo;
            Dictionary<string, AspNetEndpointParameter> parametersByName = [];
            List<string> consumes = ExtractConsumedContentTypes(endpoint.Metadata).ToList();
            List<string> produces = ExtractProducedContentTypes(endpoint.Metadata).ToList();

            if (endpoint is RouteEndpoint routeEndpoint)
            {
                routePattern = routeEndpoint.RoutePattern.RawText;

                foreach (RoutePatternParameterPart parameter in routeEndpoint.RoutePattern.Parameters)
                {
                    parametersByName[parameter.Name] = ToAspNetEndpointParameter(parameter);
                }
            }

            foreach (HandlerParameterDescriptor descriptor in handlerDescriptor.Parameters)
            {
                // Merge parameters from route pattern to discover default values (they cannot be expressed in routes).
                // Aside from that, the action method may contain additional parameters not present in the route pattern (which applies to all action methods on the page).
                AspNetEndpointParameter parameter = ToAspNetEndpointParameter(descriptor);
                MergeWithExistingParameters(parameter, parametersByName);
            }

            yield return new AspNetEndpoint(AspNetEndpointSource.RazorPagesEndpointDataSource, displayName, routePattern, httpMethods, handlerMethod,
                parametersByName.Values.ToList(), consumes, produces);
        }
    }

    private static AspNetEndpoint FromMvcControllerEndpoint(MicrosoftEndpoint endpoint)
    {
        string displayName = endpoint.DisplayName ?? string.Empty;
        string? routePattern = null;
        List<string> httpMethods = [];
        MethodInfo? handlerMethod = GetHandlerMethod(endpoint);
        Dictionary<string, AspNetEndpointParameter> parametersByName = [];
        List<string> consumes = ExtractConsumedContentTypes(endpoint.Metadata).ToList();
        List<string> produces = ExtractProducedContentTypes(endpoint.Metadata).ToList();

        if (endpoint is RouteEndpoint routeEndpoint)
        {
            routePattern = routeEndpoint.RoutePattern.RawText;
            httpMethods.AddRange(endpoint.Metadata.OfType<IHttpMethodMetadata>().SelectMany(meta => meta.HttpMethods));

            foreach (RoutePatternParameterPart parameter in routeEndpoint.RoutePattern.Parameters)
            {
                parametersByName[parameter.Name] = ToAspNetEndpointParameter(parameter);
            }
        }

        var controllerActionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();

        if (controllerActionDescriptor != null)
        {
            foreach (ParameterDescriptor descriptor in controllerActionDescriptor.Parameters)
            {
                // Merge parameters from route pattern to discover default values (they cannot be expressed in routes) and correct nullability.
                AspNetEndpointParameter parameter = ToAspNetEndpointParameter(descriptor);
                MergeWithExistingParameters(parameter, parametersByName);
            }
        }

        return new AspNetEndpoint(AspNetEndpointSource.MvcControllerEndpointDataSource, displayName, routePattern, httpMethods, handlerMethod,
            parametersByName.Values.ToList(), consumes, produces);
    }

    private static MethodInfo? GetHandlerMethod(MicrosoftEndpoint endpoint)
    {
        return endpoint.Metadata.OfType<ControllerActionDescriptor>().FirstOrDefault()?.MethodInfo ?? endpoint.Metadata.OfType<MethodInfo>().FirstOrDefault();
    }

    private static MethodInfo? GetHandlerMethod(ApiDescription description)
    {
        return description.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor
            ? controllerActionDescriptor.MethodInfo
            : description.ActionDescriptor.EndpointMetadata.OfType<MethodInfo>().FirstOrDefault();
    }

    private static string GetRouteTemplate(ApiDescription description)
    {
        return description.ActionDescriptor.AttributeRouteInfo?.Template ?? $"/{description.RelativePath}";
    }

    private static AspNetEndpointParameter ToAspNetEndpointParameter(ApiParameterDescription description)
    {
        AspNetEndpointParameterOrigin origin = ToAspNetEndpointParameterOrigin(description.Source);
        string parameterName = description.BindingInfo?.BinderModelName ?? description.Name;
        return new AspNetEndpointParameter(origin, parameterName, description.DefaultValue, description.IsRequired);
    }

    private static AspNetEndpointParameter ToAspNetEndpointParameter(HandlerParameterDescriptor descriptor)
    {
        BindingSource? source = descriptor.BindingInfo?.BindingSource;
        AspNetEndpointParameterOrigin origin = ToAspNetEndpointParameterOrigin(source);
        string parameterName = descriptor.BindingInfo?.BinderModelName ?? descriptor.Name;
        bool? isRequired = IsParameterRequired(descriptor.ParameterInfo);

        return new AspNetEndpointParameter(origin, parameterName, descriptor.ParameterInfo.DefaultValue, isRequired);
    }

    private static AspNetEndpointParameter ToAspNetEndpointParameter(ParameterDescriptor descriptor)
    {
        BindingSource? source = descriptor.BindingInfo?.BindingSource;
        AspNetEndpointParameterOrigin origin = ToAspNetEndpointParameterOrigin(source);
        string parameterName = descriptor.BindingInfo?.BinderModelName ?? descriptor.Name;

        object? defaultValue = null;
        bool? isRequired = null;

        if (descriptor is ControllerParameterDescriptor controllerDescriptor)
        {
            defaultValue = controllerDescriptor.ParameterInfo.DefaultValue;
            isRequired = IsParameterRequired(controllerDescriptor.ParameterInfo);
        }

        return new AspNetEndpointParameter(origin, parameterName, defaultValue, isRequired);
    }

    private static AspNetEndpointParameter ToAspNetEndpointParameter(RoutePatternParameterPart part)
    {
        return new AspNetEndpointParameter(AspNetEndpointParameterOrigin.Route, part.Name, part.Default, !part.IsOptional);
    }

    private static AspNetEndpointParameterOrigin ToAspNetEndpointParameterOrigin(BindingSource? source)
    {
        if (source == null)
        {
            return AspNetEndpointParameterOrigin.Unknown;
        }

        if (source == BindingSource.Path)
        {
            return AspNetEndpointParameterOrigin.Route;
        }

        if (source == BindingSource.Query)
        {
            return AspNetEndpointParameterOrigin.QueryString;
        }

        if (source == BindingSource.Header)
        {
            return AspNetEndpointParameterOrigin.Header;
        }

        return AspNetEndpointParameterOrigin.Other;
    }

    private static bool? IsParameterRequired(ParameterInfo parameter)
    {
        // Not entirely accurate, we'd need to walk up the type hierarchy; this is best-effort to cover the common cases.
        if (parameter.GetCustomAttribute<RequiredAttribute>() != null || parameter.GetCustomAttribute<BindRequiredAttribute>() != null)
        {
            return true;
        }

        if (parameter.HasDefaultValue)
        {
            return false;
        }

        NullabilityInfoContext nullabilityContext = new();
        NullabilityInfo nullabilityInfo = nullabilityContext.Create(parameter);

        return nullabilityInfo.WriteState switch
        {
            NullabilityState.NotNull => true,
            NullabilityState.Nullable => false,
            _ => null
        };
    }

    private static void MergeWithExistingParameters(AspNetEndpointParameter parameter, Dictionary<string, AspNetEndpointParameter> existingParametersByName)
    {
        if (!existingParametersByName.TryGetValue(parameter.Name, out AspNetEndpointParameter? existingParameter))
        {
            existingParametersByName.Add(parameter.Name, parameter);
        }
        else
        {
            existingParametersByName[parameter.Name] = existingParameter.MergeWith(parameter);
        }
    }

    private static IEnumerable<string> ExtractConsumedContentTypes(EndpointMetadataCollection metadata)
    {
        return metadata.GetOrderedMetadata<IAcceptsMetadata>().SelectMany(accepts => accepts.ContentTypes);
    }

    private static IEnumerable<string> ExtractProducedContentTypes(EndpointMetadataCollection metadata)
    {
        foreach (string contentType in metadata.GetOrderedMetadata<ProducesAttribute>().SelectMany(attribute => attribute.ContentTypes))
        {
            yield return contentType;
        }

        foreach (string contentType in metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().SelectMany(produces => produces.ContentTypes))
        {
            yield return contentType;
        }

        foreach (string contentType in metadata.GetOrderedMetadata<ProducesResponseTypeAttribute>()
            .SelectMany(attribute => ProducesContentTypesPropertyGetter?.Invoke(attribute, []) as MediaTypeCollection ?? []))
        {
            yield return contentType;
        }
    }
}
