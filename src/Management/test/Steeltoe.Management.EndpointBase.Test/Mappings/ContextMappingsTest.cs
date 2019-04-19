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

using Steeltoe.Management.Endpoint.Test;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Mappings.Test
{
    public class ContextMappingsTest : BaseTest
    {
        [Fact]
        public void Constructor_SetsValues()
        {
            var mappingList = new List<MappingDescription>();
            var mappingDict = new Dictionary<string, IList<MappingDescription>>()
            {
                { "dispatcherServlet", mappingList }
            };
            var contextMappings = new ContextMappings(mappingDict);
            var smappings = contextMappings.Mappings;
            Assert.Contains("dispatcherServlets", smappings.Keys);
            var mappings = smappings["dispatcherServlets"];
            Assert.Contains("dispatcherServlet", mappings.Keys);
            Assert.Same(mappingList, mappings["dispatcherServlet"]);
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
            var result = Serialize(contextMappings);
            Assert.Equal("{\"mappings\":{\"dispatcherServlets\":{\"controllerTypeName\":[{\"handler\":\"foobar\",\"predicate\":\"{[/Home/Index],methods=[GET],produces=[application/json],consumes=[application/json]}\"}]}}}", result);
        }
    }
}
