// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Owin.Testing;
using Newtonsoft.Json;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Info.Contributor;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointOwin.Test;
using System.Collections.Generic;
using System.Net;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.Info.Test
{
    public class InfoEndpointOwinMiddlewareTest : BaseTest
    {
        [Fact]
        public async void InfoInvoke_ReturnsExpected()
        {
            // arrange
            var opts = new InfoEndpointOptions();
            var ep = new TestInfoEndpoint(opts, new List<IInfoContributor>() { new GitInfoContributor() });
            var mgmtOpts = TestHelper.GetManagementOptions(opts);
            var middle = new EndpointOwinMiddleware<Dictionary<string, object>>(null, ep, mgmtOpts);
            var context = OwinTestHelpers.CreateRequest("GET", "/cloudfoundryapplication/info");

            // act
            var json = await middle.InvokeAndReadResponse(context);

            // assert
            Assert.Equal("{}", json);
        }

        [Fact]
        public async void InfoHttpCall_ReturnsExpected()
        {
            // Note: This test pulls in from git.properties and appsettings created
            // in the Startup class
            ManagementOptions.Clear();
            using (var server = TestServer.Create<Startup>())
            {
                var client = server.HttpClient;
                var result = await client.GetAsync("http://localhost/cloudfoundryapplication/infomanagement");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                var json = await result.Content.ReadAsStringAsync();
                Assert.NotNull(json);
                var dict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(json);
                Assert.NotNull(dict);

                Assert.Equal(3, dict.Count);
                Assert.True(dict.ContainsKey("application"));
                Assert.True(dict.ContainsKey("NET"));
                Assert.True(dict.ContainsKey("git"));

                var appNode = dict["application"] as Dictionary<string, object>;
                Assert.NotNull(appNode);
                Assert.Equal("foobar", appNode["name"]);

                var netNode = dict["NET"] as Dictionary<string, object>;
                Assert.NotNull(netNode);
                Assert.Equal("Core", netNode["type"]);

                var gitNode = dict["git"] as Dictionary<string, object>;
                Assert.NotNull(gitNode);
                Assert.True(gitNode.ContainsKey("build"));
                Assert.True(gitNode.ContainsKey("branch"));
                Assert.True(gitNode.ContainsKey("commit"));
                Assert.True(gitNode.ContainsKey("closest"));
                Assert.True(gitNode.ContainsKey("dirty"));
                Assert.True(gitNode.ContainsKey("remote"));
                Assert.True(gitNode.ContainsKey("tags"));
            }
        }

        [Fact]
        public void InfoEndpointMiddleware_PathAndVerbMatching_ReturnsExpected()
        {
            var opts = new InfoEndpointOptions();
            var mgmtOpts = TestHelper.GetManagementOptions(opts);

            var contribs = new List<IInfoContributor>() { new GitInfoContributor() };
            var ep = new InfoEndpoint(opts, contribs);
            var middle = new EndpointOwinMiddleware<Dictionary<string, object>>(null, ep, mgmtOpts);

            Assert.True(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/info"));
            Assert.False(middle.RequestVerbAndPathMatch("PUT", "/cloudfoundryapplication/info"));
            Assert.False(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/badpath"));
        }
    }
}
