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

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Mappings;
using Steeltoe.Management.Endpoint.Security;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Description;
using System.Web.Http.WebHost;
using System.Web.Mvc;
using System.Web.Routing;

namespace Steeltoe.Management.Endpoint.Handler
{
    public class MappingsHandler : ActuatorHandler
    {
        protected IMappingsOptions _options;
        protected MappingsEndpoint _endpoint;
        protected IApiExplorer _apiExplorer;

        public MappingsHandler(MappingsEndpoint endpoint, IEnumerable<ISecurityService> securityServices, IApiExplorer apiExplorer, IEnumerable<IManagementOptions> mgmtOptions, ILogger<MappingsHandler> logger = null)
           : base(securityServices, mgmtOptions, null, true, logger)
        {
            _options = endpoint.Options;
            _endpoint = endpoint;
            _apiExplorer = apiExplorer;
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public MappingsHandler(MappingsOptions options, IEnumerable<ISecurityService> securityServices, IApiExplorer apiExplorer, ILogger<MappingsHandler> logger = null)
            : base(securityServices, null, true, logger)
        {
            _options = options;
            _apiExplorer = apiExplorer;
        }

        public override bool RequestVerbAndPathMatch(string httpMethod, string requestPath)
        {
            _logger?.LogTrace("RequestVerbAndPathMatch {httpMethod}/{requestPath}/{optionsPath} request", httpMethod, requestPath, _options.Path);
            if (_endpoint == null)
            {
                return requestPath.Equals(_options.Path) && _allowedMethods.Any(m => m.Method.Equals(httpMethod));
            }
            else
            {
                return _endpoint.RequestVerbAndPathMatch(httpMethod, requestPath, _allowedMethods, _mgmtOptions, exactMatch: true);
            }
        }

        public async override Task<bool> IsAccessAllowed(HttpContextBase context)
        {
            return await _securityServices.IsAccessAllowed(context, _options).ConfigureAwait(false);
        }

        public override void HandleRequest(HttpContextBase context)
        {
            _logger?.LogTrace("Processing {SteeltoeEndpoint} request", typeof(MappingsHandler));

            ApplicationMappings result = GetApplicationMappings();
            var serialInfo = Serialize(result);
            _logger?.LogDebug("Returning: {0}", serialInfo);

            context.Response.Headers.Set("Content-Type", "application/vnd.spring-boot.actuator.v2+json");
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.Write(serialInfo);
        }

        protected internal ApplicationMappings GetApplicationMappings()
        {
            IDictionary<string, IList<MappingDescription>> desc = new Dictionary<string, IList<MappingDescription>>();

            // Use the IApiExplorer to get mappings for WebApi endpoints
            if (_apiExplorer != null)
            {
                desc = GetMappingDescriptions(_apiExplorer.ApiDescriptions);
            }

            // RouteTable contains routes for everything
            var routeCollection = RouteTable.Routes;
            if (routeCollection != null)
            {
                AddRouteMappingsDescriptions(routeCollection, desc);
            }

            var contextMappings = new ContextMappings(desc);
            return new ApplicationMappings(contextMappings);
        }

        protected internal IDictionary<string, IList<MappingDescription>> GetMappingDescriptions(Collection<ApiDescription> apiDescriptors)
        {
            // The apiDescriptors should have descriptors for both attribute and conventional WebAPI routes
            IDictionary<string, IList<MappingDescription>> mappingDescriptions = new Dictionary<string, IList<MappingDescription>>();
            foreach (var desc in apiDescriptors)
            {
                var adesc = desc.ActionDescriptor as ReflectedHttpActionDescriptor;
                var details = GetRouteDetails(desc);

                mappingDescriptions.TryGetValue(adesc.ControllerDescriptor.ControllerType.FullName, out IList<MappingDescription> mapList);

                if (mapList == null)
                {
                    mapList = new List<MappingDescription>();
                    mappingDescriptions.Add(adesc.ControllerDescriptor.ControllerType.FullName, mapList);
                }

                var mapDesc = new MappingDescription(adesc.MethodInfo, details);
                mapList.Add(mapDesc);
            }

            return mappingDescriptions;
        }

        protected internal IRouteDetails GetRouteDetails(ApiDescription desc)
        {
            var routeDetails = new AspNetRouteDetails
            {
                HttpMethods = GetHttpMethods(desc)
            };
            if (desc.Route?.RouteTemplate != null)
            {
                routeDetails.RouteTemplate = "/" + desc.Route?.RouteTemplate;
            }
            else if (desc.ActionDescriptor?.ControllerDescriptor != null)
            {
                routeDetails.RouteTemplate = $"/{desc.ActionDescriptor.ControllerDescriptor.ControllerName}/{desc.ActionDescriptor.ActionName}";
            }

            List<string> produces = new List<string>();
            foreach (var respTypes in desc.SupportedResponseFormatters)
            {
                foreach (var format in respTypes.SupportedMediaTypes)
                {
                    produces.Add(format.MediaType);
                }
            }

            routeDetails.Produces = produces;

            List<string> consumes = new List<string>();
            foreach (var reqTypes in desc.SupportedRequestBodyFormatters)
            {
                foreach (var format in reqTypes.SupportedMediaTypes)
                {
                    produces.Add(format.MediaType);
                }
            }

            routeDetails.Consumes = consumes;

            return routeDetails;
        }

        protected internal void AddRouteMappingsDescriptions(RouteCollection routes, IDictionary<string, IList<MappingDescription>> desc)
        {
            if (routes == null)
            {
                return;
            }

            // The RouteCollection will contain routes for everything (WebAPI, MVC, etc).
            // Since we already processed WebAPI routes, we try to ignore those as we process each route
            foreach (var router in routes)
            {
                if (router is Route route)
                {
                    var details = GetRouteDetails(route);

                    // If we are able to get an ActionDescriptor from this route
                    // then it is a MVC route that is based on attributes and we can extract the controller
                    // and controller method the route is tied to
                    var actionDesc = TryGetActionDescriptor(route);
                    if (actionDesc != null)
                    {
                        var refActionDesc = GetReflectedActionDescription(actionDesc);
                        if (refActionDesc != null)
                        {
                            var attrs = refActionDesc.GetCustomAttributes(false);
                            details.HttpMethods = GetHttpMethods(attrs);

                            desc.TryGetValue(refActionDesc.ControllerDescriptor.ControllerType.FullName, out IList<MappingDescription> mapList);

                            if (mapList == null)
                            {
                                mapList = new List<MappingDescription>();
                                desc.Add(refActionDesc.ControllerDescriptor.ControllerType.FullName, mapList);
                            }

                            var mapDesc = new MappingDescription(refActionDesc.MethodInfo, details);
                            mapList.Add(mapDesc);
                        }
                    }
                    else
                    {
                        var handler = route.RouteHandler;

                        // Ignore WebApi handler routes as the ApiExplorer already provided those mappings
                        if (handler != null && !(handler is HttpControllerRouteHandler))
                        {
                            var handlerType = handler.GetType().ToString();
                            desc.TryGetValue(handlerType, out IList<MappingDescription> mapList);

                            if (mapList == null)
                            {
                                mapList = new List<MappingDescription>();
                                desc.Add(handlerType, mapList);
                            }

                            var mapDesc = new MappingDescription("IHttpHandler.ProcessRequest(HttpContext context)", details);
                            mapList.Add(mapDesc);
                        }
                    }
                }
            }
        }

        protected internal AspNetRouteDetails GetRouteDetails(Route route)
        {
            var routeDetails = new AspNetRouteDetails();

            if (route.Url.StartsWith("/"))
            {
                routeDetails.RouteTemplate = route.Url;
            }
            else
            {
                routeDetails.RouteTemplate = "/" + route.Url;
            }

            return routeDetails;
        }

        protected bool PathMatch(string requestPath)
        {
            // TODO: Remove in 3.0
            if (_mgmtOptions == null)
            {
                return requestPath.Equals(_options.Path);
            }

            return _mgmtOptions.Select(opt => $"{opt.Path}/{_options.Id}").Any(p => p.Equals(requestPath));
        }

        private IList<string> GetHttpMethods(ApiDescription desc)
        {
            if (!string.IsNullOrEmpty(desc.HttpMethod.Method))
            {
                return new List<string>() { desc.HttpMethod.Method };
            }

            return null;
        }

        private IList<string> GetHttpMethods(object[] attributesOnActionMethod)
        {
            List<string> results = new List<string>();
            foreach (var attr in attributesOnActionMethod)
            {
                Type attrType = attr.GetType();
                string method = GetHttpMethodForType(attrType);
                if (method != null)
                {
                    results.Add(method);
                }
            }

            if (results.Count > 0)
            {
                return results;
            }

            return null;
        }

        private ActionDescriptor[] TryGetActionDescriptor(Route route)
        {
            if (route.DataTokens == null)
            {
                return null;
            }

            if (route.DataTokens.TryGetValue("MS_DirectRouteActions", out object actionDescriptor))
            {
                return actionDescriptor as ActionDescriptor[];
            }

            return null;
        }

        private ReflectedActionDescriptor GetReflectedActionDescription(ActionDescriptor[] actionDesc)
        {
           if (actionDesc.Length > 0)
            {
                return actionDesc[0] as ReflectedActionDescriptor;
            }

            return null;
        }

        private string GetHttpMethodForType(Type type)
        {
            if (type == typeof(HttpDeleteAttribute))
            {
                return "DELETE";
            }
            else if (type == typeof(HttpGetAttribute))
            {
                return "GET";
            }
            else if (type == typeof(HttpHeadAttribute))
            {
                return "HEAD";
            }
            else if (type == typeof(HttpOptionsAttribute))
            {
                return "OPTIONS";
            }
            else if (type == typeof(HttpPostAttribute))
            {
                return "POST";
            }
            else if (type == typeof(HttpPutAttribute))
            {
                return "PUT";
            }

            return null;
        }
    }
}
