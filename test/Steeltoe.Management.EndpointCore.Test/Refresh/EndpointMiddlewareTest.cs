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
using System.Net;
using System.Text;
using Xunit;

namespace Steeltoe.Management.Endpoint.Refresh.Test
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
            ["management:endpoints:path"] = "/cloudfoundryapplication"
        };

        [Fact]
        public async void HandleRefreshRequestAsync_ReturnsExpected()
        {
            var opts = new RefreshEndpointOptions();
            var mopts = TestHelpers.GetManagementOptions(opts);

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appSettings);
            var config = configurationBuilder.Build();

            var ep = new RefreshEndpoint(opts, config);
            var middle = new RefreshEndpointMiddleware(null, ep, mopts);

            var context = CreateRequest("GET", "/refresh");
            await middle.HandleRefreshRequestAsync(context);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
            var json = await reader.ReadLineAsync();
            var expected = "[\"management\",\"management:endpoints\",\"management:endpoints:path\",\"management:endpoints:enabled\",\"Logging\",\"Logging:LogLevel\",\"Logging:LogLevel:Steeltoe\",\"Logging:LogLevel:Pivotal\",\"Logging:LogLevel:Default\",\"Logging:IncludeScopes\"]";
            Assert.Equal(expected, json);
        }

        [Fact]
        public async void RefreshActuator_ReturnsExpectedData()
        {
            var anc_env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
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
                var result = await client.GetAsync("http://localhost/cloudfoundryapplication/refresh");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                var json = await result.Content.ReadAsStringAsync();
                var expected = "[\"urls\",\"management\",\"management:endpoints\",\"management:endpoints:path\",\"management:endpoints:enabled\",\"Logging\",\"Logging:LogLevel\",\"Logging:LogLevel:Steeltoe\",\"Logging:LogLevel:Pivotal\",\"Logging:LogLevel:Default\",\"Logging:IncludeScopes\",\"environment\",\"applicationName\"]";
                Assert.Equal(expected, json);
            }

            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", anc_env);
        }

        [Fact]
        public void RefreshEndpointMiddleware_PathAndVerbMatching_ReturnsExpected()
        {
            var opts = new RefreshEndpointOptions();
            var mopts = TestHelpers.GetManagementOptions(opts);
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appSettings);
            var config = configurationBuilder.Build();
            var ep = new RefreshEndpoint(opts, config);
            var middle = new RefreshEndpointMiddleware(null, ep, mopts);

            Assert.True(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/refresh"));
            Assert.False(middle.RequestVerbAndPathMatch("PUT", "/cloudfoundryapplication/refresh"));
            Assert.False(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/badpath"));
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
