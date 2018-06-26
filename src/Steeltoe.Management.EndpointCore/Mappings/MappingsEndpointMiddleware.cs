// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
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
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Middleware;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Mappings
{
    public class MappingsEndpointMiddleware : EndpointMiddleware<ApplicationMappings>
    {
        private readonly RequestDelegate _next;
        private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
        private readonly IEnumerable<IApiDescriptionProvider> _apiDescriptionProviders;
        private readonly IMappingsOptions _options;

        public MappingsEndpointMiddleware(
            RequestDelegate next,
            IMappingsOptions options,
            IActionDescriptorCollectionProvider actionDescriptorCollectionProvider = null,
            IEnumerable<IApiDescriptionProvider> apiDescriptionProviders = null,
            ILogger<MappingsEndpointMiddleware> logger = null)
            : base(logger)
        {
            _next = next;
            _options = options;
            _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
            _apiDescriptionProviders = apiDescriptionProviders;
        }

        public async Task Invoke(HttpContext context)
        {
            if (IsMappingsRequest(context))
            {
                await HandleMappingsRequestAsync(context);
            }
            else
            {
                await _next(context);
            }
        }

        protected internal async Task HandleMappingsRequestAsync(HttpContext context)
        {
            ApplicationMappings result = GetApplicationMappings(context);
            var serialInfo = Serialize(result);

            logger?.LogDebug("Returning: {0}", serialInfo);
            context.Response.Headers.Add("Content-Type", "application/vnd.spring-boot.actuator.v1+json");
            await context.Response.WriteAsync(serialInfo);
        }

        protected internal ApplicationMappings GetApplicationMappings(HttpContext context)
        {
            if (_actionDescriptorCollectionProvider != null)
            {
                ApiDescriptionProviderContext apiContext = GetApiDescriptions(_actionDescriptorCollectionProvider?.ActionDescriptors?.Items);
                IDictionary<string, IList<MappingDescription>> mappingDescriptions = GetMappingDescriptions(apiContext);
                var contextMappings = new ContextMappings(mappingDescriptions);
                return new ApplicationMappings(contextMappings);
            }
            else
            {
                var contextMappings = new ContextMappings();
                return new ApplicationMappings(contextMappings);
            }
        }

        protected internal bool IsMappingsRequest(HttpContext context)
        {
            if (!context.Request.Method.Equals("GET"))
            {
                return false;
            }

            PathString path = new PathString(_options.Path);
            return context.Request.Path.Equals(path);
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
            var routeDetails = new AspNetCoreRouteDetails();

            routeDetails.HttpMethods = GetHttpMethods(desc);

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

        private IList<string> GetHttpMethods(ApiDescription desc)
        {
            if (!string.IsNullOrEmpty(desc.HttpMethod))
            {
                return new List<string>() { desc.HttpMethod };
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
