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

using Microsoft.Extensions.Primitives;
using Microsoft.Owin.Testing;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointOwin.Test;
using System;
using System.Collections.Generic;
using System.Net;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.Security.Test
{
    public class SecurityMiddlewareOwinTest : BaseTest
    {
        [Fact]
        public async void SecurityMiddleWare_ReturnsOKWhenNotSensitive()
        {
            ManagementOptions.Clear();

            Environment.SetEnvironmentVariable("management__endpoints__enabled", "true");
            Environment.SetEnvironmentVariable("management__endpoints__info__sensitive", "false");

            using (var server = TestServer.Create<Startup>())
            {
                var client = server.HttpClient;
                var result = await client.GetAsync("http://localhost/info");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            }

            using (var server = TestServer.Create<SecureStartup>())
            {
                var client = server.HttpClient;
                client.DefaultRequestHeaders.Add("X-Test-Header", "value");

                var result = await client.GetAsync("http://localhost/info");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            }
        }

        [Fact]
        public async void SecurityMiddleWare_ReturnsUnauthorizedWhenSensitive()
        {
            ManagementOptions.Clear();

            Environment.SetEnvironmentVariable("management__endpoints__enabled", "true");
            Environment.SetEnvironmentVariable("management__endpoints__info__sensitive", "true");

            using (var server = TestServer.Create<Startup>())
            {
                var client = server.HttpClient;
                var result = await client.GetAsync("http://localhost/info");
                Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
            }
        }
    }
}
