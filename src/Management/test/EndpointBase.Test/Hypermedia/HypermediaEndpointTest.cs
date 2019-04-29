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

using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Hypermedia.Test
{
    public class HypermediaEndpointTest : BaseTest
    {
        [Fact]
        public void Constructor_ThrowsOptionsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ActuatorEndpoint(null, null));
        }

        [Fact]
        public void Invoke_ReturnsExpectedLinks()
        {
            var mgmtOptions = new ActuatorManagementOptions();
            var infoOpts = new InfoEndpointOptions();
            var cloudOpts = new HypermediaEndpointOptions();
            mgmtOptions.EndpointOptions.AddRange(new List<IEndpointOptions>() { infoOpts, cloudOpts });

            var ep = new ActuatorEndpoint(cloudOpts, new List<IManagementOptions>() { mgmtOptions });

            var info = ep.Invoke("http://localhost:5000/foobar");
            Assert.NotNull(info);
            Assert.NotNull(info._links);
            Assert.True(info._links.ContainsKey("self"));
            Assert.Equal("http://localhost:5000/foobar", info._links["self"].href);
            Assert.True(info._links.ContainsKey("info"));
            Assert.Equal("http://localhost:5000/foobar/info", info._links["info"].href);
            Assert.Equal(2, info._links.Count);
        }

        [Fact]
        public void Invoke_OnlyActuatorHypermediaEndpoint_ReturnsExpectedLinks()
        {
            var cloudOpts = new HypermediaEndpointOptions();
            var mgmtOptions = new ActuatorManagementOptions();
            mgmtOptions.EndpointOptions.Add(cloudOpts);
            var ep = new ActuatorEndpoint(cloudOpts, new List<IManagementOptions> { mgmtOptions });

            var info = ep.Invoke("http://localhost:5000/foobar");
            Assert.NotNull(info);
            Assert.NotNull(info._links);
            Assert.True(info._links.ContainsKey("self"));
            Assert.Equal("http://localhost:5000/foobar", info._links["self"].href);
            Assert.Single(info._links);
        }

        [Fact]
        public void Invoke_HonorsEndpointEnabled_ReturnsExpectedLinks()
        {
            var infoOpts = new InfoEndpointOptions { Enabled = false };
            var cloudOpts = new HypermediaEndpointOptions();
            var mgmtOptions = new ActuatorManagementOptions();

            mgmtOptions.EndpointOptions.AddRange(new List<IEndpointOptions>() { infoOpts, cloudOpts });

            var ep = new ActuatorEndpoint(cloudOpts, new List<IManagementOptions> { mgmtOptions });

            var info = ep.Invoke("http://localhost:5000/foobar");
            Assert.NotNull(info);
            Assert.NotNull(info._links);
            Assert.True(info._links.ContainsKey("self"));
            Assert.Equal("http://localhost:5000/foobar", info._links["self"].href);
            Assert.False(info._links.ContainsKey("info"));
            Assert.Single(info._links);
        }

        [Fact]
        public void Invoke_CloudFoundryDisable_ReturnsExpectedLinks()
        {
            var infoOpts = new InfoEndpointOptions { Enabled = true };
            var cloudOpts = new HypermediaEndpointOptions { Enabled = false };
            var mgmtOptions = new ActuatorManagementOptions();

            mgmtOptions.EndpointOptions.AddRange(new List<IEndpointOptions>() { infoOpts, cloudOpts });

            var ep = new ActuatorEndpoint(cloudOpts, new List<IManagementOptions> { mgmtOptions });
            var info = ep.Invoke("http://localhost:5000/foobar");
            Assert.NotNull(info);
            Assert.NotNull(info._links);
            Assert.Empty(info._links);
        }
    }
}
