// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Utils.IO;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Endpoint.HeapDump.Test
{
    public class EndpointMiddlewareTest : BaseTest
    {
        private static readonly Dictionary<string, string> AppSettings = new ()
        {
            ["Logging:IncludeScopes"] = "false",
            ["Logging:LogLevel:Default"] = "Warning",
            ["Logging:LogLevel:Pivotal"] = "Information",
            ["Logging:LogLevel:Steeltoe"] = "Information",
            ["management:endpoints:enabled"] = "true",
            ["management:endpoints:heapdump:enabled"] = "true"
        };

        [Fact]
        public async Task HandleHeapDumpRequestAsync_ReturnsExpected()
        {
            var opts = new HeapDumpEndpointOptions();
            var mopts = new ActuatorManagementOptions();
            mopts.EndpointOptions.Add(opts);

            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace));
            var loggerFactory = serviceCollection.BuildServiceProvider().GetService<ILoggerFactory>();

            var logger1 = loggerFactory.CreateLogger<HeapDumper>();
            var logger2 = loggerFactory.CreateLogger<HeapDumpEndpoint>();
            var logger3 = loggerFactory.CreateLogger<HeapDumpEndpointMiddleware>();
            var logger4 = loggerFactory.CreateLogger<HeapDumper>();

            var obs = new HeapDumper(opts, logger: logger1);

            var ep = new HeapDumpEndpoint(opts, obs, logger2);
            var middle = new HeapDumpEndpointMiddleware(null, ep, mopts, logger3);
            var context = CreateRequest("GET", "/heapdump");
            await middle.HandleHeapDumpRequestAsync(context);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var buffer = new byte[1024];
            await context.Response.Body.ReadAsync(buffer, default);
            Assert.NotEqual(0, buffer[0]);
        }

        [Fact]
        public async Task HeapDumpActuator_ReturnsExpectedData()
        {
            var builder = new WebHostBuilder()
            .UseStartup<Startup>()
            .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(AppSettings))
            .ConfigureLogging((webhostContext, loggingBuilder) =>
            {
                loggingBuilder.AddConfiguration(webhostContext.Configuration);
                loggingBuilder.AddDynamicConsole();
            });
            using var server = new TestServer(builder);
            var client = server.CreateClient();
            var result = await client.GetAsync("http://localhost/cloudfoundryapplication/heapdump");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);

            Assert.True(result.Content.Headers.Contains("Content-Type"));
            var contentType = result.Content.Headers.GetValues("Content-Type");
            Assert.Equal("application/octet-stream", contentType.Single());
            Assert.True(result.Content.Headers.Contains("Content-Disposition"));

            var tempFile = new TempFile();
            var fs = new FileStream(tempFile.FullPath, FileMode.Create);
            var input = await result.Content.ReadAsStreamAsync();
            await input.CopyToAsync(fs);
            fs.Close();

            var fs2 = File.Open(tempFile.FullPath, FileMode.Open);
            Assert.NotEqual(0, fs2.Length);
            fs2.Close();
        }

        [Fact]
        public void RoutesByPathAndVerb()
        {
            var options = new HeapDumpEndpointOptions();
            Assert.True(options.ExactMatch);
            Assert.Equal("/actuator/heapdump", options.GetContextPath(new ActuatorManagementOptions()));
            Assert.Equal("/cloudfoundryapplication/heapdump", options.GetContextPath(new CloudFoundryManagementOptions()));
            Assert.Null(options.AllowedVerbs);
        }

        private HttpContext CreateRequest(string method, string path)
        {
            HttpContext context = new DefaultHttpContext
            {
                TraceIdentifier = Guid.NewGuid().ToString()
            };
            context.Response.Body = new MemoryStream();
            context.Request.Method = method;
            context.Request.Path = new PathString(path);
            context.Request.Scheme = "http";
            context.Request.Host = new HostString("localhost");
            return context;
        }
    }
}
