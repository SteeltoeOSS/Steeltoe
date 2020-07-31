// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Owin.Testing;
using Newtonsoft.Json;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointOwin.Test;
using System.Net;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.CloudFoundry.Test
{
    public class CloudFoundryEndpointOwinMiddlewareTest : BaseTest
    {
        [Fact]
        public async void CloudFoundryInvoke_ReturnsExpected()
        {
            // arrange
            var opts = new CloudFoundryEndpointOptions();
            var mgmtOpts = TestHelper.GetManagementOptions(opts);
            var middle = new CloudFoundryEndpointOwinMiddleware(null, new TestCloudFoundryEndpoint(opts, mgmtOpts), mgmtOpts);
            var context = OwinTestHelpers.CreateRequest("GET", "/cloudfoundryapplication");

            // act
            var json = await middle.InvokeAndReadResponse(context);

            // assert
            Assert.Equal("{\"type\":\"steeltoe\",\"_links\":{}}", json);
        }

        [Fact]
        public async void CloudFoundryHttpCall_ReturnsExpected()
        {
            ManagementOptions.Clear();
            using var server = TestServer.Create<Startup>();
            var client = server.HttpClient;
            var result = await client.GetAsync("http://localhost/cloudfoundryapplication");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var json = await result.Content.ReadAsStringAsync();
            Assert.NotNull(json);
#pragma warning disable CS0618 // Type or member is obsolete
            var links = JsonConvert.DeserializeObject<Links>(json);
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.NotNull(links);
            Assert.True(links._links.ContainsKey("self"), "Self is one of the available links");
            Assert.Equal("http://localhost/cloudfoundryapplication", links._links["self"].href);
            Assert.True(links._links.ContainsKey("info"), "Info is one of the available links");
            Assert.Equal("http://localhost/cloudfoundryapplication/info", links._links["info"].href);
            Assert.False(links._links["info"].templated);

            // this test is here to prevent breaking changes in response serialization
            Assert.Equal("{\"type\":\"steeltoe\",\"_links\":{\"self\":{\"href\":\"http://localhost/cloudfoundryapplication\",\"templated\":false},\"info\":{\"href\":\"http://localhost/cloudfoundryapplication/info\",\"templated\":false}}}", json);
        }

        [Fact]
        public void CloudFoundryEndpointMiddleware_PathAndVerbMatching_ReturnsExpected()
        {
            var opts = new CloudFoundryEndpointOptions();
            var mgmtOptions = TestHelper.GetManagementOptions(opts);
            var ep = new CloudFoundryEndpoint(opts, mgmtOptions);
            var middle = new CloudFoundryEndpointOwinMiddleware(null, ep, mgmtOptions);

            Assert.True(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication"));
            Assert.False(middle.RequestVerbAndPathMatch("PUT", "/cloudfoundryapplication"));
            Assert.False(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/badpath"));
        }
    }
}