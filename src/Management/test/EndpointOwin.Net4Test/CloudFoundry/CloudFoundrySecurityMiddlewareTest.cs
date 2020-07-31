// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Primitives;
using Microsoft.Owin.Testing;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointOwin.Test;
using System;
using System.Net;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.CloudFoundry.Test
{
    public class CloudFoundrySecurityMiddlewareTest : BaseTest
    {
        private readonly SecurityBase _base = new SecurityBase(new CloudFoundryEndpointOptions(), new CloudFoundryManagementOptions());

        public CloudFoundrySecurityMiddlewareTest()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", "somestuff");
            ManagementOptions.Clear();
        }

        [Fact]
        public async void Incomplete_Config_ReturnsServiceUnavailable()
        {
            using (var server = TestServer.Create<StartupWithSecurity>())
            {
                var client = server.HttpClient;
                var result = await client.GetAsync("http://localhost/cloudfoundryapplication/info");
                Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
                var response = await result.Content.ReadAsStringAsync();
                Assert.Contains(_base.APPLICATION_ID_MISSING_MESSAGE, response);
            }

            Environment.SetEnvironmentVariable("VCAP__APPLICATION__APPLICATION_ID", "foobar");
            using (var server = TestServer.Create<StartupWithSecurity>())
            {
                var client = server.HttpClient;
                var result = await client.GetAsync("http://localhost/cloudfoundryapplication/info");
                Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
                var response = await result.Content.ReadAsStringAsync();
                Assert.Contains(_base.CLOUDFOUNDRY_API_MISSING_MESSAGE, response);
            }
        }

        [Fact]
        public async void No_AuthHeader_ReturnsUnauthorized()
        {
            Environment.SetEnvironmentVariable("VCAP__APPLICATION__APPLICATION_ID", "foobar");
            Environment.SetEnvironmentVariable("VCAP__APPLICATION__CF_API", "http://localhost:9999/foo");
            using var server = TestServer.Create<StartupWithSecurity>();
            var client = server.HttpClient;
            var result = await client.GetAsync("http://localhost/cloudfoundryapplication/info");
            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
            var response = await result.Content.ReadAsStringAsync();
            Assert.Contains(_base.AUTHORIZATION_HEADER_INVALID, response);
        }

        [Fact]
        public async void SkipsSecurityCheck_WhenEnabledFalse()
        {
            Environment.SetEnvironmentVariable("management__endpoints__cloudfoundry__enabled", "false");

            using var server = TestServer.Create<StartupWithSecurity>();
            var client = server.HttpClient;
            var result = await client.GetAsync("http://localhost/cloudfoundryapplication/info");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var response = await result.Content.ReadAsStringAsync();
            var expected = "{\"git\":{\"branch\":\"924aabdad9eb1da7bfe5b075f9befa2d0b2374e8\",\"build\":{\"host\":\"DESKTOP-K6I8LTH\",\"time\":1499884839000,\"user\":{\"email\":\"dtillman@pivotal.io\",\"name\":\"Dave Tillman\"},\"version\":\"1.5.4.RELEASE\"},\"closest\":{\"tag\":{\"commit\":{\"count\":\"10772\"},\"name\":\"v2.0.0.M2\"}},\"commit\":{\"id\":\"924aabdad9eb1da7bfe5b075f9befa2d0b2374e8\",\"message\":{\"full\":\"Release version 1.5.4.RELEASE\",\"short\":\"Release version 1.5.4.RELEASE\"},\"time\":1496926022000,\"user\":{\"email\":\"buildmaster@springframework.org\",\"name\":\"Spring Buildmaster\"}},\"dirty\":\"true\",\"remote\":{\"origin\":{\"url\":\"https://github.com/spring-projects/spring-boot.git\"}},\"tags\":\"v1.5.4.RELEASE\"},\"application\":{\"date\":\"5/1/2008\",\"name\":\"foobar\",\"time\":\"8:30:52 AM\",\"version\":\"1.0.0\"},\"NET\":{\"ASPNET\":{\"type\":\"Core\",\"version\":\"2.0.0\"},\"type\":\"Core\",\"version\":\"2.0.0\"}}";
            Assert.Equal(expected, response);
        }

        [Fact]
        public void GetAccessToken_ReturnsExpected()
        {
            var opts = new CloudFoundryEndpointOptions();
            var mgmtOptions = TestHelper.GetManagementOptions(opts);
            var middle = new CloudFoundrySecurityOwinMiddleware(null, opts,  mgmtOptions, null);
            var context = OwinTestHelpers.CreateRequest("GET", "/");
            var token = middle.GetAccessToken(context.Request);
            Assert.Null(token);

            var context2 = OwinTestHelpers.CreateRequest("GET", "/");
            context2.Request.Headers.Add("Authorization", new StringValues("Bearer foobar"));
            var token2 = middle.GetAccessToken(context2.Request);
            Assert.Equal("foobar", token2);
        }

        [Fact]
        public async void GetPermissions_ReturnsExpected()
        {
            var opts = new CloudFoundryEndpointOptions();
            var mgmtOptions = TestHelper.GetManagementOptions(opts);
            var middle = new CloudFoundrySecurityOwinMiddleware(null, opts, mgmtOptions, null);
            var context = OwinTestHelpers.CreateRequest("GET", "/");
            var result = await middle.GetPermissions(context);
            Assert.NotNull(result);
            Assert.Equal(Endpoint.Security.Permissions.NONE, result.Permissions);
            Assert.Equal(HttpStatusCode.Unauthorized, result.Code);
        }

        public override void Dispose()
        {
            base.Dispose();
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP__APPLICATION__APPLICATION_ID", null);
            Environment.SetEnvironmentVariable("management__endpoints__cloudfoundry__enabled", null);
            Environment.SetEnvironmentVariable("VCAP__APPLICATION__APPLICATION_ID", null);
            Environment.SetEnvironmentVariable("VCAP__APPLICATION__CF_API", null);
        }
    }
}
