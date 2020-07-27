// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Owin.Testing;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.EndpointOwin.Test;
using System.Collections.Generic;
using System.Net;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.ThreadDump.Test
{
    public class ThreadDumpEndpointOwinMiddlewareTest : BaseTest
    {
        public ThreadDumpEndpointOwinMiddlewareTest()
        {
            ManagementOptions.Clear();
        }

        [Fact]
        public async void ThreadDumpInvoke_ReturnsExpected()
        {
            // arrange
            var opts = new ThreadDumpEndpointOptions();
            var mgmtOptions = TestHelper.GetManagementOptions(opts);
            var middle = new EndpointOwinMiddleware<List<ThreadInfo>>(null, new ThreadDumpEndpoint(opts, new ThreadDumper(opts)), mgmtOptions);
            var context = OwinTestHelpers.CreateRequest("GET", "/cloudfoundryapplication/dump");

            // act
            var json = await middle.InvokeAndReadResponse(context);

            // assert (that it looks kinda like what we expect... ?)
            Assert.StartsWith("[", json);
            Assert.Contains("blockedCount", json);
            Assert.Contains("blockedTime", json);
            Assert.Contains("lockedMonitors", json);
            Assert.Contains("lockedSynchronizers", json);
            Assert.Contains("lockInfo", json);
            Assert.Contains("stackTrace", json);
            Assert.EndsWith("]", json);
        }

        [Fact]
        public async void ThreadDumpHttpCall_ReturnsExpected()
        {
            using var server = TestServer.Create<Startup>();
            var client = server.HttpClient;
            var result = await client.GetAsync("http://localhost/cloudfoundryapplication/dump");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var json = await result.Content.ReadAsStringAsync();
            Assert.NotNull(json);
            Assert.NotEqual("[]", json);
            Assert.StartsWith("[", json);
            Assert.Contains("blockedCount", json);
            Assert.Contains("blockedTime", json);
            Assert.Contains("lockedMonitors", json);
            Assert.Contains("lockedSynchronizers", json);
            Assert.Contains("lockInfo", json);
            Assert.Contains("stackTrace", json);
            Assert.EndsWith("]", json);
        }

        [Fact]
        public void ThreadDumpEndpointMiddleware_PathAndVerbMatching_ReturnsExpected()
        {
            var opts = new ThreadDumpEndpointOptions();
            var mgmtOptions = TestHelper.GetManagementOptions(opts);
            var obs = new ThreadDumper(opts);
            var ep = new ThreadDumpEndpoint(opts, obs);
            var middle = new EndpointOwinMiddleware<List<ThreadInfo>>(null, ep, mgmtOptions);

            Assert.True(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/dump"));
            Assert.False(middle.RequestVerbAndPathMatch("PUT", "/cloudfoundryapplication/dump"));
            Assert.False(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/badpath"));
        }
    }
}
