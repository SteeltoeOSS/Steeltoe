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

namespace Steeltoe.Management.Endpoint.Metrics.Test
{
    public class MetricsListNamesResponseTest : BaseTest
    {
        [Fact]
        public void Constructor_SetsValues()
        {
            var names = new HashSet<string>()
            {
                "foo.bar",
                "bar.foo"
            };
            var resp = new MetricsListNamesResponse(names);
            Assert.NotNull(resp.Names);
            Assert.Same(names, resp.Names);
        }

        [Fact]
        public void JsonSerialization_ReturnsExpected()
        {
            var names = new HashSet<string>()
            {
                "foo.bar",
                "bar.foo"
            };
            var resp = new MetricsListNamesResponse(names);
            var result = Serialize(resp);
            Assert.Equal("{\"names\":[\"foo.bar\",\"bar.foo\"]}", result);
        }
    }
}
