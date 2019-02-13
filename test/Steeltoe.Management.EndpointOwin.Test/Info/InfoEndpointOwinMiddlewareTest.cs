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
            var ep = new TestInfoEndpoint(new InfoOptions(), new List<IInfoContributor>() { new GitInfoContributor() });
            var middle = new EndpointOwinMiddleware<Dictionary<string, object>>(null, ep);
            var context = OwinTestHelpers.CreateRequest("GET", "/info");

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
            var opts = new InfoOptions();
            var contribs = new List<IInfoContributor>() { new GitInfoContributor() };
            var ep = new InfoEndpoint(opts, contribs);
            var middle = new EndpointOwinMiddleware<Dictionary<string, object>>(null, ep);

            Assert.True(middle.RequestVerbAndPathMatch("GET", "/info"));
            Assert.False(middle.RequestVerbAndPathMatch("PUT", "/info"));
            Assert.False(middle.RequestVerbAndPathMatch("GET", "/badpath"));
        }
    }
}
