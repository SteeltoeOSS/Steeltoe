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

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Xunit;

namespace Steeltoe.Management.Endpoint.HeapDump.Test
{
    public class EndpointMiddlewareTest : BaseTest
    {
        private static Dictionary<string, string> appSettings = new Dictionary<string, string>()
        {
            ["Logging:IncludeScopes"] = "false",
            ["Logging:LogLevel:Default"] = "Warning",
            ["Logging:LogLevel:Pivotal"] = "Information",
            ["Logging:LogLevel:Steeltoe"] = "Information",
            ["management:endpoints:enabled"] = "true",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:heapdump:enabled"] = "true"
        };

        [Fact]
        public async void HandleHeapDumpRequestAsync_ReturnsExpected()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var opts = new HeapDumpOptions();
                LoggerFactory loggerFactory = new LoggerFactory();
                loggerFactory.AddConsole(minLevel: LogLevel.Debug);
                var logger1 = loggerFactory.CreateLogger<HeapDumper>();
                var logger2 = loggerFactory.CreateLogger<HeapDumpEndpoint>();
                var logger3 = loggerFactory.CreateLogger<HeapDumpEndpointMiddleware>();

                HeapDumper obs = new HeapDumper(opts, logger: logger1);
                var ep = new HeapDumpEndpoint(opts, obs, logger2);
                var middle = new HeapDumpEndpointMiddleware(null, ep, logger3);
                var context = CreateRequest("GET", "/heapdump");
                await middle.HandleHeapDumpRequestAsync(context);
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                byte[] buffer = new byte[1024];
                await context.Response.Body.ReadAsync(buffer, 0, 1024);
                Assert.NotEqual(0, buffer[0]);
            }
        }

        [Fact]
        public async void HeapDumpActuator_ReturnsExpectedData()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var builder = new WebHostBuilder()
                .UseStartup<Startup>()
                .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(appSettings))
                .ConfigureLogging((webhostContext, loggingBuilder) =>
                {
                    loggingBuilder.AddConfiguration(webhostContext.Configuration);
                    loggingBuilder.AddDynamicConsole();
                });
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
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
            var middle = new HeapDumpEndpointMiddleware(null, ep);

            Assert.True(middle.RequestVerbAndPathMatch("GET", "/heapdump"));
            Assert.False(middle.RequestVerbAndPathMatch("PUT", "/heapdump"));
            Assert.False(middle.RequestVerbAndPathMatch("GET", "/badpath"));
        }

        private HttpContext CreateRequest(string method, string path)
        {
            HttpContext context = new DefaultHttpContext();
            context.TraceIdentifier = Guid.NewGuid().ToString();
            context.Response.Body = new MemoryStream();
            context.Request.Method = method;
            context.Request.Path = new PathString(path);
            context.Request.Scheme = "http";
            context.Request.Host = new HostString("localhost");
            return context;
        }
    }
}
