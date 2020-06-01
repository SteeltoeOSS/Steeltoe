// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Owin.Testing;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointOwin.Test;
using System;
using System.IO;
using System.Linq;
using System.Net;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.HeapDump.Test
{
    public class HeapDumpEndpointOwinMiddlewareTest : BaseTest
    {
        public HeapDumpEndpointOwinMiddlewareTest()
        {
            ManagementOptions.Clear();
        }

        [Fact]
        public async void HeapDumpInvoke_ReturnsExpected()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var opts = new HeapDumpEndpointOptions();
                var mopts = TestHelper.GetManagementOptions(opts);
                var loggerFactory = TestHelpers.GetLoggerFactory();
                var logger1 = loggerFactory.CreateLogger<HeapDumper>();
                var logger2 = loggerFactory.CreateLogger<HeapDumpEndpoint>();
                var logger3 = loggerFactory.CreateLogger<HeapDumpEndpointOwinMiddleware>();

                HeapDumper obs = new HeapDumper(opts, logger: logger1);
                var ep = new HeapDumpEndpoint(opts, obs, logger2);
                var middle = new HeapDumpEndpointOwinMiddleware(null, ep, mopts, logger3);
                var context = OwinTestHelpers.CreateRequest("GET", "/cloudfoundryapplication/heapdump", GetResponseBodyStream());
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
            var opts = new HeapDumpEndpointOptions();
            var mopts = TestHelper.GetManagementOptions(opts);
            HeapDumper obs = new HeapDumper(opts);
            var ep = new HeapDumpEndpoint(opts, obs);
            var middle = new HeapDumpEndpointOwinMiddleware(null, ep, mopts);

            Assert.True(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/heapdump"));
            Assert.False(middle.RequestVerbAndPathMatch("PUT", "/cloudfoundryapplication/heapdump"));
            Assert.False(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/badpath"));
        }

        private Stream GetResponseBodyStream()
        {
            FileStream stream = new FileStream(Path.GetTempFileName(), FileMode.Create);
            return stream;
        }
    }
}
