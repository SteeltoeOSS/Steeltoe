// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.EndpointCore.ContentNegotiation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Mappings
{
    public class MappingsEndpointMiddleware : EndpointMiddleware<ApplicationMappings>
    {
        private readonly RequestDelegate _next;
        private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
        private readonly IEnumerable<IApiDescriptionProvider> _apiDescriptionProviders;
        private readonly IMappingsOptions _options;
        private readonly IRouteMappings _routeMappings;

        public MappingsEndpointMiddleware(
            RequestDelegate next,
            IMappingsOptions options,
            IEnumerable<IManagementOptions> mgmtOptions,
            IRouteMappings routeMappings = null,
            IActionDescriptorCollectionProvider actionDescriptorCollectionProvider = null,
            IEnumerable<IApiDescriptionProvider> apiDescriptionProviders = null,
            ILogger<MappingsEndpointMiddleware> logger = null)
            : base(mgmtOptions, logger: logger)
        {
            _next = next;
            _options = options;
            _routeMappings = routeMappings;
            _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
            _apiDescriptionProviders = apiDescriptionProviders;
        }

        public Task Invoke(HttpContext context)
        {
            if (IsMappingsRequest(context))
            {
                return HandleMappingsRequestAsync(context);
            }

            return _next(context);
        }

        protected internal Task HandleMappingsRequestAsync(HttpContext context)
        {
            var result = GetApplicationMappings(context);
            var serialInfo = Serialize(result);

            _logger?.LogDebug("Returning: {0}", serialInfo);

            context.HandleContentNegotiation(_logger);
            return context.Response.WriteAsync(serialInfo);
        }

        protected internal ApplicationMappings GetApplicationMappings(HttpContext context)
        {
            IDictionary<string, IList<MappingDescription>> desc = new Dictionary<string, IList<MappingDescription>>();
            if (_actionDescriptorCollectionProvider != null)
            {
                var apiContext = GetApiDescriptions(_actionDescriptorCollectionProvider?.ActionDescriptors?.Items);
                desc = GetMappingDescriptions(apiContext);
            }

            if (_routeMappings != null)
            {
                AddRouteMappingsDescriptions(_routeMappings, desc);
            }

            var contextMappings = new ContextMappings(desc);
            return new ApplicationMappings(contextMappings);
        }

        protected internal bool IsMappingsRequest(HttpContext context)
        {
            if (!context.Request.Method.Equals("GET"))
            {
                return false;
            }

            var paths = new List<string>();
            if (_mgmtOptions != null)
            {
                paths.AddRange(_mgmtOptions.Select(opt => $"{opt.Path}/{_options.Id}"));
            }
            else
            {
                paths.Add(_options.Path);
            }

            foreach (var path in paths)
            {
                var pathString = new PathString(path);
                if (context.Request.Path.Equals(pathString))
                {
                    return true;
                }
            }

            return false;
        }

        protected internal IDictionary<string, IList<MappingDescription>> GetMappingDescriptions(ApiDescriptionProviderContext apiContext)
        {
            IDictionary<string, IList<MappingDescription>> mappingDescriptions = new Dictionary<string, IList<MappingDescription>>();
            foreach (var desc in apiContext.Results)
            {
                var cdesc = desc.ActionDescriptor as ControllerActionDescriptor;
                var details = GetRouteDetails(desc);
                mappingDescriptions.TryGetValue(cdesc.ControllerTypeInfo.FullName, out var mapList);

                if (mapList == null)
                {
                    mapList = new List<MappingDescription>();
                    mappingDescriptions.Add(cdesc.ControllerTypeInfo.FullName, mapList);
                }

                var mapDesc = new MappingDescription(cdesc.MethodInfo, details);
                mapList.Add(mapDesc);
            }

            foreach (var desc in apiContext.Actions)
            {
                if (desc is ControllerActionDescriptor cdesc)
                {
                    if (apiContext.Results.Any() && mappingDescriptions.Any(description => description.Value.Any(n => n.Handler.Equals(cdesc.MethodInfo.ToString()))))
                    {
                        continue;
                    }

                    var details = GetRouteDetails(desc);
                    mappingDescriptions.TryGetValue(cdesc.ControllerTypeInfo.FullName, out var mapList);

                    if (mapList == null)
                    {
                        mapList = new List<MappingDescription>();
                        mappingDescriptions.Add(cdesc.ControllerTypeInfo.FullName, mapList);
                    }

                    var mapDesc = new MappingDescription(cdesc.MethodInfo, details);
                    mapList.Add(mapDesc);
                }
            }

            return mappingDescriptions;
        }

        protected internal IRouteDetails GetRouteDetails(ApiDescription desc)
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
                var cdesc = desc.ActionDescriptor as ControllerActionDescriptor;
                routeDetails.RouteTemplate = $"/{cdesc.ControllerName}/{cdesc.ActionName}";
            }

            var produces = new List<string>();
            foreach (var respTypes in desc.SupportedResponseTypes)
            {
                foreach (var format in respTypes.ApiResponseFormats)
                {
                    produces.Add(format.MediaType);
                }
            }

            routeDetails.Produces = produces;

            var consumes = new List<string>();
            foreach (var reqTypes in desc.SupportedRequestFormats)
            {
                consumes.Add(reqTypes.MediaType);
            }

            routeDetails.Consumes = consumes;

            return routeDetails;
        }

        protected internal IRouteDetails GetRouteDetails(ActionDescriptor desc)
        {
            var routeDetails = new AspNetCoreRouteDetails
            {
                HttpMethods = desc.ActionConstraints?.OfType<HttpMethodActionConstraint>().SingleOrDefault()?.HttpMethods.ToList() ?? new List<string> { MappingDescription.ALL_HTTP_METHODS },
                Consumes = new List<string>(),
                Produces = new List<string>()
            };

            if (desc.AttributeRouteInfo?.Template != null)
            {
                routeDetails.RouteTemplate = desc.AttributeRouteInfo.Template;
            }
            else
            {
                var cdesc = desc as ControllerActionDescriptor;
                routeDetails.RouteTemplate = $"/{cdesc.ControllerName}/{cdesc.ActionName}";
            }

            foreach (var filter in desc.FilterDescriptors.Where(f => f.Filter is ProducesAttribute).Select(f => (ProducesAttribute)f.Filter))
            {
                foreach (var format in filter.ContentTypes)
                {
                    routeDetails.Produces.Add(format);
                }
            }

            foreach (var filter in desc.FilterDescriptors.Where(f => f.Filter is ConsumesAttribute).Select(f => (ConsumesAttribute)f.Filter))
            {
                foreach (var format in filter.ContentTypes)
                {
                    routeDetails.Consumes.Add(format);
                }
            }

            return routeDetails;
        }

        protected internal IRouteDetails GetRouteDetails(Route route)
        {
            var routeDetails = new AspNetCoreRouteDetails
            {
                HttpMethods = GetHttpMethods(route),
                RouteTemplate = route.RouteTemplate
            };

            return routeDetails;
        }

        protected internal void AddRouteMappingsDescriptions(IRouteMappings routeMappings, IDictionary<string, IList<MappingDescription>> desc)
        {
            if (routeMappings == null)
            {
                return;
            }

            foreach (var router in routeMappings.Routers)
            {
                if (router is Route route)
                {
                    var details = GetRouteDetails(route);
                    desc.TryGetValue("CoreRouteHandler", out var mapList);

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
                return new List<string>() { desc.HttpMethod };
            }

            return null;
        }

        private IList<string> GetHttpMethods(Route route)
        {
            var constraints = route.Constraints;
            if (constraints.TryGetValue("httpMethod", out var routeConstraint) && routeConstraint is HttpMethodRouteConstraint methodConstraint)
            {
                return methodConstraint.AllowedMethods;
            }

            return null;
        }

        private ApiDescriptionProviderContext GetApiDescriptions(IReadOnlyList<ActionDescriptor> actionDescriptors)
        {
            if (actionDescriptors == null)
            {
                return new ApiDescriptionProviderContext(new List<ActionDescriptor>());
            }

            var context = new ApiDescriptionProviderContext(actionDescriptors);
            foreach (var provider in _apiDescriptionProviders)
            {
                provider.OnProvidersExecuting(context);
            }

            return context;
        }
    }
}
