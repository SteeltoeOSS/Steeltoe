// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

            var ep = new ActuatorEndpoint(cloudOpts, mgmtOptions);

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
        public void Invoke_OnlyActuatorHypermediaEndpoint_ReturnsExpectedLinks()
        {
            var cloudOpts = new HypermediaEndpointOptions();
            var mgmtOptions = new ActuatorManagementOptions();
            mgmtOptions.EndpointOptions.Add(cloudOpts);
            var ep = new ActuatorEndpoint(cloudOpts, mgmtOptions);

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
            var infoOpts = new InfoEndpointOptions { Enabled = false };
            var cloudOpts = new HypermediaEndpointOptions();
            var mgmtOptions = new ActuatorManagementOptions();

            mgmtOptions.EndpointOptions.AddRange(new List<IEndpointOptions>() { infoOpts, cloudOpts });

            var ep = new ActuatorEndpoint(cloudOpts, mgmtOptions);

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
            var infoOpts = new InfoEndpointOptions { Enabled = true };
            var cloudOpts = new HypermediaEndpointOptions { Enabled = false };
            var mgmtOptions = new ActuatorManagementOptions();

            mgmtOptions.EndpointOptions.AddRange(new List<IEndpointOptions>() { infoOpts, cloudOpts });

            var ep = new ActuatorEndpoint(cloudOpts, mgmtOptions);
            var info = ep.Invoke("http://localhost:5000/foobar");
            Assert.NotNull(info);
            Assert.NotNull(info._links);
            Assert.Empty(info._links);
        }
    }
}
