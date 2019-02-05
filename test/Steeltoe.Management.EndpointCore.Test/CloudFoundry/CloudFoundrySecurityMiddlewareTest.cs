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
using Microsoft.Extensions.Primitives;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Xunit;

namespace Steeltoe.Management.Endpoint.CloudFoundry.Test
{
    public class CloudFoundrySecurityMiddlewareTest : BaseTest
    {
        private Dictionary<string, string> appSettings = new Dictionary<string, string>()
        {
            ["management:endpoints:enabled"] = "true",
            ["management:endpoints:sensitive"] = "false",
            ["management:endpoints:path"] = "/",
            ["management:endpoints:info:enabled"] = "true",
            ["management:endpoints:info:sensitive"] = "false",
            ["info:application:name"] = "foobar",
            ["info:application:version"] = "1.0.0",
            ["info:application:date"] = "5/1/2008",
            ["info:application:time"] = "8:30:52 AM",
            ["info:NET:type"] = "Core",
            ["info:NET:version"] = "2.0.0",
            ["info:NET:ASPNET:type"] = "Core",
            ["info:NET:ASPNET:version"] = "2.0.0"
        };

        public CloudFoundrySecurityMiddlewareTest()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", "somestuff");
        }

        [Fact]
        public async void CloudFoundrySecurityMiddleware_ReturnsServiceUnavailable()
        {
            var appSettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "true",
                ["management:endpoints:sensitive"] = "false",
                ["management:endpoints:path"] = "/",
                ["management:endpoints:info:enabled"] = "true",
                ["management:endpoints:info:sensitive"] = "false",
                ["info:application:name"] = "foobar",
                ["info:application:version"] = "1.0.0",
                ["info:application:date"] = "5/1/2008",
                ["info:application:time"] = "8:30:52 AM",
                ["info:NET:type"] = "Core",
                ["info:NET:version"] = "2.0.0",
                ["info:NET:ASPNET:type"] = "Core",
                ["info:NET:ASPNET:version"] = "2.0.0"
            };

            var builder = new WebHostBuilder()
                .UseStartup<StartupWithSecurity>()
                .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(appSettings));

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/cloudfoundryapplication/info");
                Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
            }

            var appSettings2 = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "true",
                ["management:endpoints:sensitive"] = "false",
                ["management:endpoints:path"] = "/",
                ["management:endpoints:info:enabled"] = "true",
                ["management:endpoints:info:sensitive"] = "false",
                ["info:application:name"] = "foobar",
                ["info:application:version"] = "1.0.0",
                ["info:application:date"] = "5/1/2008",
                ["info:application:time"] = "8:30:52 AM",
                ["info:NET:type"] = "Core",
                ["info:NET:version"] = "2.0.0",
                ["info:NET:ASPNET:type"] = "Core",
                ["info:NET:ASPNET:version"] = "2.0.0",
                ["vcap:application:application_id"] = "foobar"
            };

            var builder2 = new WebHostBuilder().UseStartup<StartupWithSecurity>()
                .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(appSettings2));

            using (var server = new TestServer(builder2))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/cloudfoundryapplication/info");
                Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
            }

            var appSettings3 = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "true",
                ["management:endpoints:sensitive"] = "false",
                ["management:endpoints:path"] = "/",
                ["management:endpoints:info:enabled"] = "true",
                ["management:endpoints:info:sensitive"] = "false",
                ["info:application:name"] = "foobar",
                ["info:application:version"] = "1.0.0",
                ["info:application:date"] = "5/1/2008",
                ["info:application:time"] = "8:30:52 AM",
                ["info:NET:type"] = "Core",
                ["info:NET:version"] = "2.0.0",
                ["info:NET:ASPNET:type"] = "Core",
                ["info:NET:ASPNET:version"] = "2.0.0",
                ["vcap:application:application_id"] = "foobar",
                ["vcap:application:cf_api"] = "http://localhost:9999/foo"
            };

            var builder3 = new WebHostBuilder().UseStartup<StartupWithSecurity>()
                .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(appSettings3));

            using (var server = new TestServer(builder3))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/cloudfoundryapplication/barfoo");
                Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
            }
        }

        [Fact]
        public async void CloudFoundrySecurityMiddleware_ReturnsSecurityException()
        {
            var appSettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "true",
                ["management:endpoints:sensitive"] = "false",
                ["management:endpoints:path"] = "/",
                ["management:endpoints:info:enabled"] = "true",
                ["management:endpoints:info:sensitive"] = "false",
                ["info:application:name"] = "foobar",
                ["info:application:version"] = "1.0.0",
                ["info:application:date"] = "5/1/2008",
                ["info:application:time"] = "8:30:52 AM",
                ["info:NET:type"] = "Core",
                ["info:NET:version"] = "2.0.0",
                ["info:NET:ASPNET:type"] = "Core",
                ["info:NET:ASPNET:version"] = "2.0.0",
                ["vcap:application:application_id"] = "foobar",
                ["vcap:application:cf_api"] = "http://localhost:9999/foo"
            };

            var builder = new WebHostBuilder()
                .UseStartup<StartupWithSecurity>()
                .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(appSettings));

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/cloudfoundryapplication/info");
                Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
            }
        }

        [Fact]
        public async void CloudFoundrySecurityMiddleware_SkipsSecurityCheckIfEnabledFalse()
        {
            var appSettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "true",
                ["management:endpoints:sensitive"] = "false",
                ["management:endpoints:path"] = "/",
                ["management:endpoints:info:enabled"] = "true",
                ["management:endpoints:info:sensitive"] = "false",
                ["management:endpoints:cloudfoundry:enabled"] = "false",
                ["info:application:name"] = "foobar",
                ["info:application:version"] = "1.0.0",
                ["info:application:date"] = "5/1/2008",
                ["info:application:time"] = "8:30:52 AM",
                ["info:NET:type"] = "Core",
                ["info:NET:version"] = "2.0.0",
                ["info:NET:ASPNET:type"] = "Core",
                ["info:NET:ASPNET:version"] = "2.0.0",
                ["vcap:application:application_id"] = "foobar",
                ["vcap:application:cf_api"] = "http://localhost:9999/foo"
            };

            var builder = new WebHostBuilder()
                .UseStartup<StartupWithSecurity>()
                .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(appSettings));

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/info");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            }
        }

        [Fact]
        public async void CloudFoundrySecurityMiddleware_SkipsSecurityCheckIfEnabledFalseViaEnvVariables()
        {
            Environment.SetEnvironmentVariable("MANAGEMENT__ENDPOINTS__CLOUDFOUNDRY__ENABLED", "False");
            var appSettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "true",
                ["management:endpoints:sensitive"] = "false",
                ["management:endpoints:path"] = "/",
                ["management:endpoints:info:enabled"] = "true",
                ["management:endpoints:info:sensitive"] = "false",
                ["info:application:name"] = "foobar",
                ["info:application:version"] = "1.0.0",
                ["info:application:date"] = "5/1/2008",
                ["info:application:time"] = "8:30:52 AM",
                ["info:NET:type"] = "Core",
                ["info:NET:version"] = "2.0.0",
                ["info:NET:ASPNET:type"] = "Core",
                ["info:NET:ASPNET:version"] = "2.0.0",
                ["vcap:application:application_id"] = "foobar",
                ["vcap:application:cf_api"] = "http://localhost:9999/foo"
            };

            var builder = new WebHostBuilder()
                .UseStartup<StartupWithSecurity>()
                .ConfigureAppConfiguration((builderContext, config) =>
                {
                    config.AddInMemoryCollection(appSettings);
                    config.AddEnvironmentVariables();
                });

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/info");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            }
        }

        [Fact]
        public void GetAccessToken_ReturnsExpected()
        {
            var opts = new CloudFoundryOptions();
            var middle = new CloudFoundrySecurityMiddleware(null, opts, null);
            var context = CreateRequest("GET", "/");
            var token = middle.GetAccessToken(context.Request);
            Assert.Null(token);

            var context2 = CreateRequest("GET", "/");
            context2.Request.Headers.Add("Authorization", new StringValues("Bearer foobar"));
            var token2 = middle.GetAccessToken(context2.Request);
            Assert.Equal("foobar", token2);
        }

        [Fact]
        public async void GetPermissions_ReturnsExpected()
        {
            var opts = new CloudFoundryOptions();
            var middle = new CloudFoundrySecurityMiddleware(null, opts, null);
            var context = CreateRequest("GET", "/");
            var result = await middle.GetPermissions(context);
            Assert.NotNull(result);
            Assert.Equal(Security.Permissions.NONE, result.Permissions);
            Assert.Equal(HttpStatusCode.Unauthorized, result.Code);
        }

        public override void Dispose()
        {
            base.Dispose();
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("MANAGEMENT__ENDPOINTS__CLOUDFOUNDRY__ENABLED", null);
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
