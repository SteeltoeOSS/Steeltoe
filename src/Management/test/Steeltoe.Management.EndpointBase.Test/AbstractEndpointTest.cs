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

using Steeltoe.Management.Endpoint.Security;
using System;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test
{
    public class AbstractEndpointTest : BaseTest
    {
        [Fact]
        public void ThrowsIfOptionsull()
        {
            Assert.Throws<ArgumentNullException>(() => new TestEndpoint(null));
        }

        [Fact]
        public void ReturnsOptionValues()
        {
            var ep = new TestEndpoint(new TestOptions() { Id = "foo", Enabled = false });
            Assert.False(ep.Enabled);
            Assert.Equal("foo", ep.Id);
            Assert.Equal(0, ep.Invoke());
        }
    }
}
