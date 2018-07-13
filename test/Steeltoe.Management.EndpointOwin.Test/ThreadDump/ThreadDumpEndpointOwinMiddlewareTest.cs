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
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.EndpointOwin.Test;
using System.Collections.Generic;
using System.Net;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.ThreadDump.Test
{
    public class ThreadDumpEndpointOwinMiddlewareTest : OwinBaseTest
    {
        [Fact]
        public async void ThreadDumpInvoke_ReturnsExpected()
        {
            // arrange
            var opts = new ThreadDumpOptions();
            var middle = new EndpointOwinMiddleware<List<ThreadInfo>>(null, new ThreadDumpEndpoint(opts, new ThreadDumper(opts)));
            var context = OwinTestHelpers.CreateRequest("GET", "/dump");

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
            using (var server = TestServer.Create<Startup>())
            {
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
        }

        [Fact]
        public void ThreadDumpEndpointMiddleware_PathAndVerbMatching_ReturnsExpected()
        {
            var opts = new ThreadDumpOptions();
            ThreadDumper obs = new ThreadDumper(opts);
            var ep = new ThreadDumpEndpoint(opts, obs);
            var middle = new EndpointOwinMiddleware<List<ThreadInfo>>(null, ep);

            Assert.True(middle.RequestVerbAndPathMatch("GET", "/dump"));
            Assert.False(middle.RequestVerbAndPathMatch("PUT", "/dump"));
            Assert.False(middle.RequestVerbAndPathMatch("GET", "/badpath"));
        }
    }
}
