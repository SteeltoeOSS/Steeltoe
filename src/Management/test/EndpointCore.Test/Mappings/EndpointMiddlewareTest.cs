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

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Xunit;

namespace Steeltoe.Management.Endpoint.Mappings.Test
{
    public class EndpointMiddlewareTest : BaseTest
    {
        private static readonly Dictionary<string, string> AppSettings = new Dictionary<string, string>()
        {
            ["Logging:IncludeScopes"] = "false",
            ["Logging:LogLevel:Default"] = "Warning",
            ["Logging:LogLevel:Pivotal"] = "Information",
            ["Logging:LogLevel:Steeltoe"] = "Information",
            ["management:endpoints:enabled"] = "true",
            ["management:endpoints:path"] = "/cloudfoundryapplication"
        };

        [Fact]
        public void IsMappingsRequest_ReturnsExpected()
        {
            var opts = new MappingsEndpointOptions();
            var mopts = TestHelper.GetManagementOptions(opts);

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(AppSettings);
            var config = configurationBuilder.Build();
            var host = HostingHelpers.GetHostingEnvironment();
            var middle = new MappingsEndpointMiddleware(null, opts, mopts);

            var context = CreateRequest("GET", "/cloudfoundryapplication/mappings");
            Assert.True(middle.IsMappingsRequest(context));
            var context2 = CreateRequest("PUT", "/cloudfoundryapplication/mappings");
            Assert.False(middle.IsMappingsRequest(context2));
            var context3 = CreateRequest("GET", "/cloudfoundryapplication/badpath");
            Assert.False(middle.IsMappingsRequest(context3));
        }

        [Fact]
        public async void HandleMappingsRequestAsync_MVCNotUsed_NoRoutes_ReturnsExpected()
        {
            var opts = new MappingsEndpointOptions();
            var mopts = TestHelper.GetManagementOptions(opts);

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(AppSettings);
            var config = configurationBuilder.Build();
            var host = HostingHelpers.GetHostingEnvironment();
            var middle = new MappingsEndpointMiddleware(null, opts, mopts);

            var context = CreateRequest("GET", "/cloudfoundryapplication/mappings");
            await middle.HandleMappingsRequestAsync(context);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
            var json = await reader.ReadLineAsync();
            var expected = "{\"contexts\":{\"application\":{\"mappings\":{\"dispatcherServlets\":{\"dispatcherServlet\":[]}}}}}";
            Assert.Equal(expected, json);
        }

        [Fact]
        public async void MappingsActuator_ReturnsExpectedData()
        {
            var builder = new WebHostBuilder()
            .UseStartup<Startup>()
            .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(AppSettings))
            .ConfigureLogging((webhostContext, loggingBuilder) =>
            {
                loggingBuilder.AddConfiguration(webhostContext.Configuration);
                loggingBuilder.AddDynamicConsole();
            });
            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
#if NETCOREAPP3_0
                await Assert.ThrowsAsync<NotImplementedException>(() => client.GetAsync("http://localhost/cloudfoundryapplication/mappings"));
#else
                var result = await client.GetAsync("http://localhost/cloudfoundryapplication/mappings");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                var json = await result.Content.ReadAsStringAsync();
                var expected = "{\"contexts\":{\"application\":{\"mappings\":{\"dispatcherServlets\":{\"Steeltoe.Management.EndpointCore.Mappings.Test.HomeController\":[{\"handler\":\"Steeltoe.Management.EndpointCore.Mappings.Test.Person Index()\",\"predicate\":\"{[/Home/Index],methods=[GET],produces=[text/plain || application/json || text/json]}\"}]}}}}}";
                Assert.Equal(expected, json);
#endif
            }
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
