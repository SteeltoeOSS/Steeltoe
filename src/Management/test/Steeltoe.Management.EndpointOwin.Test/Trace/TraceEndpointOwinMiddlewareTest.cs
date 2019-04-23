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
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.Endpoint.Trace;
using Steeltoe.Management.EndpointOwin.Test;
using System.Collections.Generic;
using System.Net;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.Trace.Test
{
    public class TraceEndpointOwinMiddlewareTest : BaseTest
    {
        public TraceEndpointOwinMiddlewareTest()
        {
            ManagementOptions.Clear();
        }

        [Fact]
        public async void TraceInvoke_ReturnsExpected()
        {
            // arrange
            var opts = new TraceEndpointOptions();
            var mgmtOptions = TestHelpers.GetManagementOptions(opts);
            var ep = new TestTraceEndpoint(opts, new TraceDiagnosticObserver(opts));
            var middle = new EndpointOwinMiddleware<List<TraceResult>>(null, ep, mgmtOptions);
            var context = OwinTestHelpers.CreateRequest("GET", "/cloudfoundryapplication/trace");

            // act
            var json = await middle.InvokeAndReadResponse(context);

            // assert
            Assert.Equal("[]", json);
        }

        [Fact]
        public async void TraceHttpCall_ReturnsExpectedData()
        {
            using (var server = TestServer.Create<Startup>())
            {
                var client = server.HttpClient;

                // first request will not have any traces as none have completed yet...
                var emptyResult = await client.GetAsync("http://localhost/cloudfoundryapplication/trace");
                var json = await emptyResult.Content.ReadAsStringAsync();
                Assert.Equal(HttpStatusCode.OK, emptyResult.StatusCode);
                Assert.Equal("[]", json);

                // that was boring, let's go retrieve a trace of that first request!
                var secondResult = await client.GetAsync("http://localhost/cloudfoundryapplication/trace");
                json = await secondResult.Content.ReadAsStringAsync();
                /* sample response: [{"timestamp":1530892771663,"info":{"method":"GET","path":"/cloudfoundryapplication/trace","headers":{"request":{"host":"localhost"},"response":{"content-type":"application/vnd.spring-boot.actuator.v1+json","status":"200"}},"timeTaken":"221"}}] */
                Assert.Equal(HttpStatusCode.OK, secondResult.StatusCode);
                Assert.Contains("\"path\":\"/cloudfoundryapplication/trace\"", json);
                Assert.Contains("\"method\":\"GET\"", json);
                Assert.Contains("\"content-type\":\"application/vnd.spring-boot.actuator.v1+json\"", json);
            }
        }

        [Fact]
        public void TraceEndpointMiddleware_PathAndVerbMatching_ReturnsExpected()
        {
            var opts = new TraceEndpointOptions();
            var mopts = TestHelpers.GetManagementOptions(opts);
            var obs = new TraceDiagnosticObserver(opts);
            var ep = new TraceEndpoint(opts, obs);
            var middle = new EndpointOwinMiddleware<List<TraceResult>>(null, ep, mopts);

            Assert.True(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/trace"));
            Assert.False(middle.RequestVerbAndPathMatch("PUT", "/cloudfoundryapplication/trace"));
            Assert.False(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/badpath"));
        }
    }
}
