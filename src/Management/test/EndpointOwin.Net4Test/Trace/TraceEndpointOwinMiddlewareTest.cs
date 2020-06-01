// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            var mgmtOptions = TestHelper.GetManagementOptions(opts);
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
            var mopts = TestHelper.GetManagementOptions(opts);
            var obs = new TraceDiagnosticObserver(opts);
            var ep = new TraceEndpoint(opts, obs);
            var middle = new EndpointOwinMiddleware<List<TraceResult>>(null, ep, mopts);

            Assert.True(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/trace"));
            Assert.False(middle.RequestVerbAndPathMatch("PUT", "/cloudfoundryapplication/trace"));
            Assert.False(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/badpath"));
        }
    }
}
