// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Middleware;
using System;
using System.Collections.Generic;
using System.IO;
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

        [Obsolete]
        public MappingsEndpointMiddleware(
            RequestDelegate next,
            IMappingsOptions options,
            IRouteMappings routeMappings = null,
            IActionDescriptorCollectionProvider actionDescriptorCollectionProvider = null,
            IEnumerable<IApiDescriptionProvider> apiDescriptionProviders = null,
            ILogger<MappingsEndpointMiddleware> logger = null)
            : base(logger: logger)
        {
            _next = next;
            _options = options;
            _routeMappings = routeMappings;
            _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
            _apiDescriptionProviders = apiDescriptionProviders;
        }

        public async Task Invoke(HttpContext context)
        {
            if (IsMappingsRequest(context))
            {
                await HandleMappingsRequestAsync(context).ConfigureAwait(false);
            }
            else
            {
                await _next(context).ConfigureAwait(false);
            }
        }

        protected internal async Task HandleMappingsRequestAsync(HttpContext context)
        {
            ApplicationMappings result = GetApplicationMappings(context);
            var serialInfo = Serialize(result);

            _logger?.LogDebug("Returning: {0}", serialInfo);
            context.Response.Headers.Add("Content-Type", "application/vnd.spring-boot.actuator.v2+json");
            await context.Response.WriteAsync(serialInfo).ConfigureAwait(false);
        }

        protected internal ApplicationMappings GetApplicationMappings(HttpContext context)
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
                PathString pathString = new PathString(path);
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
                mappingDescriptions.TryGetValue(cdesc.ControllerTypeInfo.FullName, out IList<MappingDescription> mapList);

                if (mapList == null)
                {
                    mapList = new List<MappingDescription>();
                    mappingDescriptions.Add(cdesc.ControllerTypeInfo.FullName, mapList);
                }

                var mapDesc = new MappingDescription(cdesc.MethodInfo, details);
                mapList.Add(mapDesc);
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
                ControllerActionDescriptor cdesc = desc.ActionDescriptor as ControllerActionDescriptor;
                routeDetails.RouteTemplate = $"/{cdesc.ControllerName}/{cdesc.ActionName}";
            }

            List<string> produces = new List<string>();
            foreach (var respTypes in desc.SupportedResponseTypes)
            {
                foreach (var format in respTypes.ApiResponseFormats)
                {
                    produces.Add(format.MediaType);
                }
            }

            routeDetails.Produces = produces;

            List<string> consumes = new List<string>();
            foreach (var reqTypes in desc.SupportedRequestFormats)
            {
                 consumes.Add(reqTypes.MediaType);
            }

            routeDetails.Consumes = consumes;

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

        protected internal IRouteDetails GetRouteDetails(Route route)
        {
            var routeDetails = new AspNetCoreRouteDetails
            {
                HttpMethods = GetHttpMethods(route),
                RouteTemplate = route.RouteTemplate
            };

            return routeDetails;
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
            if (constraints.TryGetValue("httpMethod", out IRouteConstraint routeConstraint) && routeConstraint is HttpMethodRouteConstraint methodConstraint)
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

            foreach (var action in actionDescriptors)
            {
                // This is required in order for OnProvidersExecuting() to work
                var apiExplorerActionData = new ApiDescriptionActionData()
                {
                    GroupName = "Steeltoe"
                };

                action.SetProperty(apiExplorerActionData);
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
