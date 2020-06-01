// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Owin;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Mappings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Description;

namespace Steeltoe.Management.EndpointOwin.Mappings
{
    public class MappingsEndpointOwinMiddleware : EndpointOwinMiddleware<ApplicationMappings>
    {
        protected IMappingsOptions _options;
        protected IApiExplorer _apiExplorer;

        public MappingsEndpointOwinMiddleware(OwinMiddleware next, IMappingsOptions options, IEnumerable<IManagementOptions> mgmtOptions, IApiExplorer apiExplorer, ILogger logger = null)
            : base(next, mgmtOptions, logger: logger)
        {
            _options = options;
            _apiExplorer = apiExplorer;
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public MappingsEndpointOwinMiddleware(OwinMiddleware next, IMappingsOptions options, IApiExplorer apiExplorer, ILogger logger = null)
            : base(next, logger: logger)
        {
            _options = options;
            _apiExplorer = apiExplorer;
        }

        public override async Task Invoke(IOwinContext context)
        {
            if (!RequestVerbAndPathMatch(context.Request.Method, context.Request.Path.Value))
            {
                await Next.Invoke(context).ConfigureAwait(false);
            }
            else
            {
                _logger?.LogTrace("Processing {SteeltoeEndpoint} request", typeof(MappingsEndpointOwinMiddleware));
                ApplicationMappings result = GetApplicationMappings();
                var serialInfo = Serialize(result);
                _logger?.LogDebug("Returning: {0}", serialInfo);
                context.Response.Headers.SetValues("Content-Type", new string[] { "application/vnd.spring-boot.actuator.v2+json" });
                await context.Response.WriteAsync(Serialize(result)).ConfigureAwait(false);
            }
        }

        public override bool RequestVerbAndPathMatch(string httpMethod, string requestPath)
        {
            var paths = new List<string>();

            if (_mgmtOptions == null)
            {
                paths.Add(_options.Path);
            }
            else
            {
                paths.AddRange(_mgmtOptions.Select(opt => $"{opt.Path}/{_options.Id}")); // TODO: Handle Path override
            }

            _logger?.LogTrace("RequestVerbAndPathMatch {httpMethod}/{requestPath}/{optionsPath} request", httpMethod, requestPath, string.Join(",", paths));
            return paths.Any(p => p.Equals(requestPath)) && _allowedMethods.Any(m => m.Method.Equals(httpMethod));
        }

        protected internal ApplicationMappings GetApplicationMappings()
        {
            IDictionary<string, IList<MappingDescription>> desc = new Dictionary<string, IList<MappingDescription>>();

            // Use the IApiExplorer to get mappings for WebApi endpoints
            if (_apiExplorer != null)
            {
                desc = GetMappingDescriptions(_apiExplorer.ApiDescriptions);
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

        private IList<string> GetHttpMethods(ApiDescription desc)
        {
            if (!string.IsNullOrEmpty(desc.HttpMethod.Method))
            {
                return new List<string>() { desc.HttpMethod.Method };
            }

            return null;
        }
    }
}
