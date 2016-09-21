//
// Copyright 2015 the original author or authors.
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
//

using System;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Xunit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Internal;
using Steeltoe.CloudFoundry.Connector.OAuth;
using Microsoft.AspNetCore.Hosting.Internal;
using System.Net;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test
{
    public class CloudFoundryAppBuilderExtensionsTest
    {
        [Fact]
        public void UseCloudFoundryAuthentication_ThowsAppBuilderNull()
        {
            // Arrange
            IApplicationBuilder builder = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => CloudFoundryAppBuilderExtensions.UseCloudFoundryAuthentication(builder));
            Assert.Contains(nameof(builder), ex.Message);
            var ex2 = Assert.Throws<ArgumentNullException>(() => CloudFoundryAppBuilderExtensions.UseCloudFoundryAuthentication(builder, new CloudFoundryOptions()));
            Assert.Contains(nameof(builder), ex2.Message);

        }

        [Fact]
        public void UseCloudFoundryAuthentication_ThowsCloudFoundryOptionsNull()
        {
            // Arrange
            IApplicationBuilder builder = new ApplicationBuilder(null);
            CloudFoundryOptions options = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => CloudFoundryAppBuilderExtensions.UseCloudFoundryAuthentication(builder, options));
            Assert.Contains(nameof(options), ex.Message);

        }

        [Fact]
        public async void UseCloudFoundryAuthentication_AddsMiddlewareIntoPipeline()
        {
            IHostingEnvironment envir = new HostingEnvironment();
            CloudFoundryOptions opts = new CloudFoundryOptions();

            TestServerStartup.CloudFoundryOptions = opts;
            TestServerStartup.ServiceOptions = null;
            var builder = new WebHostBuilder().UseStartup<TestServerStartup>().UseEnvironment("development");
            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/");
                Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
                var location = result.Headers.Location.ToString();
                Assert.True(location.StartsWith("http://default_oauthserviceurl/oauth/authorize"));
            }

        }

        [Fact]
        public async void UseCloudFoundryAuthentication_UsesOAuthServiceOptionsIfPresent()
        {
            // Arrange
            OAuthServiceOptions serviceOptions = new OAuthServiceOptions()
            {
                ClientId = "ClientId",
                ClientSecret = "ClientSecret",
                UserAuthorizationUrl = "http://userauthorizationurl/oauth/authorize",
                AccessTokenUrl = "http://AccessTokenUrl",
                UserInfoUrl = "http://UserInfoUrl",
                TokenInfoUrl = "http://TokenInfoUrl",
                JwtKeyUrl = "http://JwtKeyUrl",
                Scope = { "foo", "bar" }
            };

            IHostingEnvironment envir = new HostingEnvironment();

            TestServerStartup.CloudFoundryOptions = null;
            TestServerStartup.ServiceOptions = serviceOptions;
            var builder = new WebHostBuilder().UseStartup<TestServerStartup>().UseEnvironment("development");
            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/");
                Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
                var location = result.Headers.Location.ToString();
                Assert.True(location.StartsWith("http://userauthorizationurl/oauth/authorize"));
            }

        }
        [Fact]
        public void UseCloudFoundryJwtAuthentication_ThowsAppBuilderNull()
        {
            // Arrange
            IApplicationBuilder builder = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => CloudFoundryAppBuilderExtensions.UseCloudFoundryJwtAuthentication(builder));
            Assert.Contains(nameof(builder), ex.Message);
            var ex2 = Assert.Throws<ArgumentNullException>(() => CloudFoundryAppBuilderExtensions.UseCloudFoundryJwtAuthentication(builder, new CloudFoundryOptions()));
            Assert.Contains(nameof(builder), ex2.Message);

        }

        [Fact]
        public void UseCloudFoundryJwtAuthentication_ThowsCloudFoundryOptionsNull()
        {
            // Arrange
            IApplicationBuilder builder = new ApplicationBuilder(null);
            CloudFoundryOptions options = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => CloudFoundryAppBuilderExtensions.UseCloudFoundryJwtAuthentication(builder, options));
            Assert.Contains(nameof(options), ex.Message);

        }

        [Fact]
        public async void UseCloudFoundryJwtAuthentication_AddsMiddlewareIntoPipeline()
        {
            IHostingEnvironment envir = new HostingEnvironment();
            CloudFoundryOptions opts = new CloudFoundryOptions();

            TestServerStartup.CloudFoundryOptions = opts;
            TestServerStartup.ServiceOptions = null;
            var builder = new WebHostBuilder().UseStartup<TestServerJwtStartup>().UseEnvironment("development");
            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/");
                Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
            }

        }

    }

}

