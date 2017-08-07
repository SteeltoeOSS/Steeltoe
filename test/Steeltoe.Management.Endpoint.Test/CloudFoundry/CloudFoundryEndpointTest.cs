//
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


using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Test;
using Xunit;

namespace Steeltoe.Management.Endpoint.CloudFoundry.Test
{
    public class CloudFoundryEndpointTest : BaseTest
    {
        [Fact]
        public void InvokeReturnsExpectedLinks()
        {
            var infoOpts = new InfoOptions();
            var cloudOpts = new CloudFoundryOptions();

            var ep = new CloudFoundryEndpoint(cloudOpts);

            var info = ep.Invoke("http://localhost:5000/foobar");
            Assert.NotNull(info);
            Assert.NotNull(info._links);
            Assert.True(info._links.ContainsKey("self"));
            Assert.Equal("http://localhost:5000/foobar",info._links["self"].href);
            Assert.True(info._links.ContainsKey("info"));
            Assert.Equal("http://localhost:5000/foobar/info", info._links["info"].href);
            Assert.Equal(2, info._links.Count);
        }

        [Fact]
        public void InvokeReturnsExpectedLinks2()
        {
            var cloudOpts = new CloudFoundryOptions();

            var ep = new CloudFoundryEndpoint(cloudOpts);

            var info = ep.Invoke("http://localhost:5000/foobar");
            Assert.NotNull(info);
            Assert.NotNull(info._links);
            Assert.True(info._links.ContainsKey("self"));
            Assert.Equal("http://localhost:5000/foobar", info._links["self"].href);
            Assert.Equal(1, info._links.Count);
        }
    }
}
