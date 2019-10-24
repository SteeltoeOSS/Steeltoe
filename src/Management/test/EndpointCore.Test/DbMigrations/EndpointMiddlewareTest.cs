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

using FluentAssertions;
using FluentAssertions.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointBase.DbMigrations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Xunit;

namespace Steeltoe.Management.Endpoint.DbMigrations.Test
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
        public async void HandleEntityFrameworkRequestAsync_ReturnsExpected()
        {
            var opts = new DbMigrationsEndpointOptions();

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appSettings);
            var mgmtOptions = TestHelper.GetManagementOptions(opts);
            var efContext = new MockDbContext();
            var container = Substitute.For<IServiceProvider>();
            container.GetService(typeof(MockDbContext)).Returns(efContext);
            var helper = Substitute.For<DbMigrationsEndpoint.DbMigrationsEndpointHelper>();
            helper.ScanRootAssembly.Returns(typeof(MockDbContext).Assembly);
            helper.GetPendingMigrations(Arg.Any<DbContext>()).Returns(new[] { "pending" });
            helper.GetAppliedMigrations(Arg.Any<DbContext>()).Returns(new[] { "applied" });
            var ep = new DbMigrationsEndpoint(opts, container, helper);

            var middle = new DbMigrationsEndpointMiddleware(null, ep, mgmtOptions);

            var context = CreateRequest("GET", "/dbmigrations");
            await middle.HandleEntityFrameworkRequestAsync(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
            var json = await reader.ReadToEndAsync();
            var expected = JToken.FromObject(
                new Dictionary<string, DbMigrationsDescriptor>()
                {
                    {
                        nameof(MockDbContext), new DbMigrationsDescriptor()
                        {
                            AppliedMigrations = new List<string> { "applied" },
                            PendingMigrations = new List<string> { "pending" }
                        }
                    }
                },
                GetSerializer());
            var actual = JObject.Parse(json);
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async void EntityFrameworkActuator_ReturnsExpectedData()
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
                var result = await client.GetAsync("http://localhost/cloudfoundryapplication/dbmigrations");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                var json = await result.Content.ReadAsStringAsync();
                var expected = JToken.FromObject(
                    new Dictionary<string, DbMigrationsDescriptor>()
                    {
                        {
                            nameof(MockDbContext), new DbMigrationsDescriptor()
                            {
                                AppliedMigrations = new List<string> { "applied" },
                                PendingMigrations = new List<string> { "pending" }
                            }
                        }
                    },
                    GetSerializer());
                var actual = JObject.Parse(json);
                actual.Should().BeEquivalentTo(expected);
            }
        }

        [Fact]
        public void EntityFrameworkEndpointMiddleware_PathAndVerbMatching_ReturnsExpected()
        {
            var opts = new DbMigrationsEndpointOptions();
            var efContext = new MockDbContext();
            var container = Substitute.For<IServiceProvider>();
            container.GetService(typeof(MockDbContext)).Returns(efContext);
            var helper = Substitute.For<DbMigrationsEndpoint.DbMigrationsEndpointHelper>();
            helper.GetPendingMigrations(Arg.Any<DbContext>()).Returns(new[] { "pending" });
            helper.GetAppliedMigrations(Arg.Any<DbContext>()).Returns(new[] { "applied" });
            var ep = new DbMigrationsEndpoint(opts, container, helper);

            var mgmt = new CloudFoundryManagementOptions() { Path = "/" };
            mgmt.EndpointOptions.Add(opts);
            var middle = new DbMigrationsEndpointMiddleware(null, ep, new List<IManagementOptions> { mgmt });

            middle.RequestVerbAndPathMatch("GET", "/dbmigrations").Should().BeTrue();
            middle.RequestVerbAndPathMatch("PUT", "/dbmigrations").Should().BeFalse();
            middle.RequestVerbAndPathMatch("GET", "/badpath").Should().BeFalse();
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
