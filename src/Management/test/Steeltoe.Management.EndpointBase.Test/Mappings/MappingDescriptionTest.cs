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
    public class MappingDescriptionTest : BaseTest
    {
        [Fact]
        public void Constructor_SetsValues()
        {
            var routeDetail = new TestRouteDetails()
            {
                HttpMethods = new List<string>() { "GET" },
                RouteTemplate = "/Home/Index",
                Consumes = new List<string>() { "application/json" },
                Produces = new List<string>() { "application/json" }
            };
            var mapDesc = new MappingDescription("foobar", routeDetail);

            Assert.Null(mapDesc.Details);
            Assert.Equal("foobar", mapDesc.Handler);
            Assert.Equal("{[/Home/Index],methods=[GET],produces=[application/json],consumes=[application/json]}", mapDesc.Predicate);
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
            var mapDesc = new MappingDescription("foobar", routeDetail);

            var result = Serialize(mapDesc);
            Assert.Equal("{\"handler\":\"foobar\",\"predicate\":\"{[/Home/Index],methods=[GET],produces=[application/json],consumes=[application/json]}\"}", result);
        }
    }
}
