// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Test;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Mappings.Test
{
    public class ApplicationMappingsTest : BaseTest
    {
        [Fact]
        public void Constructor_SetsValues()
        {
            var mappingDict = new Dictionary<string, IList<MappingDescription>>
            {
                { "dispatcherServlet", new List<MappingDescription>() }
            };
            var contextMappings = new ContextMappings(mappingDict);

            var appMappings = new ApplicationMappings(contextMappings);
            var ctxMappings = appMappings.ContextMappings;
            Assert.Contains("application", ctxMappings.Keys);
            Assert.Single(ctxMappings.Keys);
            Assert.Same(contextMappings, ctxMappings["application"]);
        }

        [Fact]
        public void JsonSerialization_ReturnsExpected()
        {
            var routeDetail = new TestRouteDetails
            {
                HttpMethods = new List<string> { "GET" },
                RouteTemplate = "/Home/Index",
                Consumes = new List<string> { "application/json" },
                Produces = new List<string> { "application/json" }
            };

            var mappDescs = new List<MappingDescription>
            {
                new MappingDescription("foobar", routeDetail)
            };

            var mappingDict = new Dictionary<string, IList<MappingDescription>>
            {
                { "controllerTypeName", mappDescs }
            };

            var contextMappings = new ContextMappings(mappingDict);
            var appMappings = new ApplicationMappings(contextMappings);

            var result = Serialize(appMappings);
            Assert.Equal("{\"contexts\":{\"application\":{\"mappings\":{\"dispatcherServlets\":{\"controllerTypeName\":[{\"handler\":\"foobar\",\"predicate\":\"{[/Home/Index],methods=[GET],produces=[application/json],consumes=[application/json]}\"}]}}}}}", result);
        }
    }
}
