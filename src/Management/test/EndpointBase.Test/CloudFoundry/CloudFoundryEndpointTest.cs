// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            Assert.Throws<ArgumentNullException>(() => new CloudFoundryEndpoint(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new CloudFoundryEndpoint(new CloudFoundryEndpointOptions(), null, null));
        }

        [Fact]
        public void Invoke_ReturnsExpectedLinks()
        {
            var infoOpts = new InfoEndpointOptions();
            var cloudOpts = new CloudFoundryEndpointOptions();
            var mgmtOptions = new CloudFoundryManagementOptions();
            mgmtOptions.EndpointOptions.Add(infoOpts);
            mgmtOptions.EndpointOptions.Add(cloudOpts);

            var ep = new CloudFoundryEndpoint(cloudOpts, mgmtOptions, null);

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
            var cloudOpts = new CloudFoundryEndpointOptions();
            var mgmtOptions = new CloudFoundryManagementOptions();
            mgmtOptions.EndpointOptions.Add(cloudOpts);
            var ep = new CloudFoundryEndpoint(cloudOpts, mgmtOptions);

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
            var cloudOpts = new CloudFoundryEndpointOptions();

            var mgmtOptions = new CloudFoundryManagementOptions();
            mgmtOptions.EndpointOptions.Add(infoOpts);
            mgmtOptions.EndpointOptions.Add(cloudOpts);
            var ep = new CloudFoundryEndpoint(cloudOpts, mgmtOptions);

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
            var cloudOpts = new CloudFoundryEndpointOptions { Enabled = false };
            var mgmtOptions = new CloudFoundryManagementOptions();
            mgmtOptions.EndpointOptions.Add(infoOpts);

            var ep = new CloudFoundryEndpoint(cloudOpts, mgmtOptions);

            var info = ep.Invoke("http://localhost:5000/foobar");
            Assert.NotNull(info);
            Assert.NotNull(info._links);
            Assert.Empty(info._links);
        }
    }
}
