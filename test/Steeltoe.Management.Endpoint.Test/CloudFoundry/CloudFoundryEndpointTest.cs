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
using System;
using Xunit;

namespace Steeltoe.Management.Endpoint.CloudFoundry.Test
{
    public class CloudFoundryEndpointTest : BaseTest
    {
        [Fact]
        public void Constructor_ThrowsOptionsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new CloudFoundryEndpoint(null));
        }

        [Fact]
        public void Invoke_ReturnsExpectedLinks()
        {
            var infoOpts = new InfoOptions();
            var cloudOpts = new CloudFoundryOptions();

            var ep = new CloudFoundryEndpoint(cloudOpts, null);

            var info = ep.Invoke("http://localhost:5000/foobar");
            Assert.NotNull(info);
            Assert.NotNull(info._links);
            Assert.True(info._links.ContainsKey("self"));
            Assert.Equal("http://localhost:5000/foobar", info._links["self"].Href);
            Assert.True(info._links.ContainsKey("info"));
            Assert.Equal("http://localhost:5000/foobar/info", info._links["info"].Href);
            Assert.Equal(2, info._links.Count);
        }

        [Fact]
        public void Invoke_OnlyCloudFoundryEndpoint_ReturnsExpectedLinks()
        {
            var cloudOpts = new CloudFoundryOptions();

            var ep = new CloudFoundryEndpoint(cloudOpts);

            var info = ep.Invoke("http://localhost:5000/foobar");
            Assert.NotNull(info);
            Assert.NotNull(info._links);
            Assert.True(info._links.ContainsKey("self"));
            Assert.Equal("http://localhost:5000/foobar", info._links["self"].Href);
            Assert.Single(info._links);
        }

        [Fact]
        public void Invoke_HonorsEndpointEnabled_ReturnsExpectedLinks()
        {
            var infoOpts = new InfoOptions();
            infoOpts.Enabled = false;
            var cloudOpts = new CloudFoundryOptions();

            var ep = new CloudFoundryEndpoint(cloudOpts);

            var info = ep.Invoke("http://localhost:5000/foobar");
            Assert.NotNull(info);
            Assert.NotNull(info._links);
            Assert.True(info._links.ContainsKey("self"));
            Assert.Equal("http://localhost:5000/foobar", info._links["self"].Href);
            Assert.False(info._links.ContainsKey("info"));
            Assert.Single(info._links);
        }

        [Fact]
        public void Invoke_CloudFoundryDisable_ReturnsExpectedLinks()
        {
            var infoOpts = new InfoOptions();
            infoOpts.Enabled = true;
            var cloudOpts = new CloudFoundryOptions();
            cloudOpts.Enabled = false;

            var ep = new CloudFoundryEndpoint(cloudOpts);

            var info = ep.Invoke("http://localhost:5000/foobar");
            Assert.NotNull(info);
            Assert.NotNull(info._links);
            Assert.Empty(info._links);
        }
    }
}
