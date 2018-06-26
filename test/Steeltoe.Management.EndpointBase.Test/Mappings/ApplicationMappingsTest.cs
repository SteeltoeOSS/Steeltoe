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
            var mappingDict = new Dictionary<string, IList<MappingDescription>>()
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
            var routeDetail = new TestRouteDetails()
            {
                HttpMethods = new List<string>() { "GET" },
                RouteTemplate = "/Home/Index",
                Consumes = new List<string>() { "application/json" },
                Produces = new List<string>() { "application/json" }
            };

            var mappDescs = new List<MappingDescription>()
            {
                new MappingDescription("foobar", routeDetail)
            };

            var mappingDict = new Dictionary<string, IList<MappingDescription>>()
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
