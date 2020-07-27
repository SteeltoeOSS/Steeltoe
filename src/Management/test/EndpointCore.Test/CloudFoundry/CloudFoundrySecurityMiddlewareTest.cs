// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
                ["management:endpoints:path"] = "/",
                ["management:endpoints:info:enabled"] = "true",
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
                ["management:endpoints:path"] = "/",
                ["management:endpoints:info:enabled"] = "true",
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
                ["management:endpoints:path"] = "/",
                ["management:endpoints:info:enabled"] = "true",
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

                ["management:endpoints:path"] = "/",
                ["management:endpoints:info:enabled"] = "true",

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

            using var server = new TestServer(builder);
            var client = server.CreateClient();
            var result = await client.GetAsync("http://localhost/cloudfoundryapplication/info");
            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        }

        [Fact]
        public async void CloudFoundrySecurityMiddleware_SkipsSecurityCheckIfEnabledFalse()
        {
            var appSettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "true",

                ["management:endpoints:path"] = "/",
                ["management:endpoints:info:enabled"] = "true",

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

            using var server = new TestServer(builder);
            var client = server.CreateClient();
            var result = await client.GetAsync("http://localhost/info");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }

        [Fact]
        public async void CloudFoundrySecurityMiddleware_SkipsSecurityCheckIfEnabledFalseViaEnvVariables()
        {
            Environment.SetEnvironmentVariable("MANAGEMENT__ENDPOINTS__CLOUDFOUNDRY__ENABLED", "False");
            var appSettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "true",
                ["management:endpoints:path"] = "/",
                ["management:endpoints:info:enabled"] = "true",
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

            using var server = new TestServer(builder);
            var client = server.CreateClient();
            var result = await client.GetAsync("http://localhost/info");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }

        [Fact]
        public void GetAccessToken_ReturnsExpected()
        {
            var opts = new CloudFoundryEndpointOptions();
            var mgmtOptions = TestHelper.GetManagementOptions(opts);
            var middle = new CloudFoundrySecurityMiddleware(null, opts, mgmtOptions, null);
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
            var opts = new CloudFoundryEndpointOptions();
            var mgmtOptions = TestHelper.GetManagementOptions(opts);
            var middle = new CloudFoundrySecurityMiddleware(null, opts, mgmtOptions, null);
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
