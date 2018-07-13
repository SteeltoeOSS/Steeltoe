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

using Microsoft.Extensions.Logging;
using Microsoft.Owin.Testing;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.EndpointOwin.Test;
using System;
using System.IO;
using System.Linq;
using System.Net;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.HeapDump.Test
{
    public class HeapDumpEndpointOwinMiddlewareTest : OwinBaseTest
    {
        [Fact]
        public async void HeapDumpInvoke_ReturnsExpected()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var opts = new HeapDumpOptions();
                LoggerFactory loggerFactory = new LoggerFactory();
                loggerFactory.AddConsole(minLevel: LogLevel.Debug);
                var logger1 = loggerFactory.CreateLogger<HeapDumper>();
                var logger2 = loggerFactory.CreateLogger<HeapDumpEndpoint>();
                var logger3 = loggerFactory.CreateLogger<HeapDumpEndpointOwinMiddleware>();

                HeapDumper obs = new HeapDumper(opts, logger: logger1);
                var ep = new HeapDumpEndpoint(opts, obs, logger2);
                var middle = new HeapDumpEndpointOwinMiddleware(null, ep, logger3);
                var context = OwinTestHelpers.CreateRequest("GET", "/heapdump");
                await middle.Invoke(context);
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                byte[] buffer = new byte[1024];
                await context.Response.Body.ReadAsync(buffer, 0, 1024);
                Assert.NotEqual(0, buffer[0]);
            }
        }

        [Fact]
        public async void HeapDumpHttpCall_ReturnsExpected()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                using (var server = TestServer.Create<Startup>())
                {
                    var client = server.HttpClient;
                    var result = await client.GetAsync("http://localhost/cloudfoundryapplication/heapdump");
                    Assert.Equal(HttpStatusCode.OK, result.StatusCode);

                    Assert.True(result.Content.Headers.Contains("Content-Type"));
                    var contentType = result.Content.Headers.GetValues("Content-Type");
                    Assert.Equal("application/octet-stream", contentType.Single());
                    Assert.True(result.Content.Headers.Contains("Content-Disposition"));

                    string tempFile = Path.GetTempFileName();
                    FileStream fs = new FileStream(tempFile, FileMode.Create);
                    Stream input = await result.Content.ReadAsStreamAsync();
                    await input.CopyToAsync(fs);
                    fs.Close();

                    FileStream fs2 = File.Open(tempFile, FileMode.Open);
                    Assert.NotEqual(0, fs2.Length);
                    fs2.Close();
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public void HeapDumpEndpointMiddleware_PathAndVerbMatching_ReturnsExpected()
        {
            var opts = new HeapDumpOptions();
            HeapDumper obs = new HeapDumper(opts);
            var ep = new HeapDumpEndpoint(opts, obs);
            var middle = new HeapDumpEndpointOwinMiddleware(null, ep);

            Assert.True(middle.RequestVerbAndPathMatch("GET", "/heapdump"));
            Assert.False(middle.RequestVerbAndPathMatch("PUT", "/heapdump"));
            Assert.False(middle.RequestVerbAndPathMatch("GET", "/badpath"));
        }
    }
}
