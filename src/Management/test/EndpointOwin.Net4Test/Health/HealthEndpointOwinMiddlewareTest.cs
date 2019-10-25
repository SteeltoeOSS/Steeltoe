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

using Microsoft.Owin.Testing;
using Newtonsoft.Json;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Health.Contributor;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointOwin.Test;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.Health.Test
{
    public class HealthEndpointOwinMiddlewareTest : BaseTest
    {
        public HealthEndpointOwinMiddlewareTest()
        {
            ManagementOptions.Clear();
        }

        [Fact]
        public async void HealthInvoke_ReturnsExpected()
        {
            // arrange
            var opts = new HealthEndpointOptions();
            var mopts = TestHelper.GetManagementOptions(opts);
            var contribs = new List<IHealthContributor>() { new DiskSpaceContributor() };
            var ep = new TestHealthEndpoint(opts, new DefaultHealthAggregator(), contribs);
            var middle = new HealthEndpointOwinMiddleware(null, ep, mopts);
            var context = OwinTestHelpers.CreateRequest("GET", "/cloudfoundryapplication/health");

            // act
            var json = await middle.InvokeAndReadResponse(context);

            // assert
            Assert.Equal("{\"status\":\"UNKNOWN\"}", json);
        }

        [Fact]
        public async void HealthHttpCall_ReturnsExpected()
        {
            using (var server = TestServer.Create<Startup>())
            {
                var client = server.HttpClient;

                // check the default version
                var result = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
                var health = await AssertHealthResponseAsync(HttpStatusCode.OK, HealthStatus.UP, result);

                // check the down version
                var result2 = await client.GetAsync("http://localhost/cloudfoundryapplication/down");
                await AssertHealthResponseAsync(HttpStatusCode.ServiceUnavailable, HealthStatus.DOWN, result2);

                // check the out of service
                var result3 = await client.GetAsync("http://localhost/cloudfoundryapplication/out");
                await AssertHealthResponseAsync(HttpStatusCode.ServiceUnavailable, HealthStatus.OUT_OF_SERVICE, result3);

                // check the unknown version... expect OK
                var result4 = await client.GetAsync("http://localhost/cloudfoundryapplication/unknown");
                await AssertHealthResponseAsync(HttpStatusCode.OK, HealthStatus.UNKNOWN, result4);
            }
        }

        [Fact]
        public void HealthEndpointMiddleware_PathAndVerbMatching_ReturnsExpected()
        {
            var opts = new HealthEndpointOptions();
            var mopts = TestHelper.GetManagementOptions(opts);
            var contribs = new List<IHealthContributor>() { new DiskSpaceContributor() };
            var ep = new HealthEndpoint(opts, new DefaultHealthAggregator(), contribs);
            var middle = new HealthEndpointOwinMiddleware(null, ep, mopts);

            Assert.True(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/health"));
            Assert.False(middle.RequestVerbAndPathMatch("PUT", "/cloudfoundryapplication/health"));
            Assert.False(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/badpath"));
        }

        private async Task<Dictionary<string, object>> AssertHealthResponseAsync(HttpStatusCode expectedHttpStatus, HealthStatus expectedHealthStatus, HttpResponseMessage response)
        {
            Assert.NotNull(response);
            Assert.Equal(expectedHttpStatus, response.StatusCode);
            var json = await response.Content.ReadAsStringAsync();
            var health = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.Equal(health["status"], expectedHealthStatus.ToString());
            return health;
        }
    }
}
