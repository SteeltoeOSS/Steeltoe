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

using Microsoft.Extensions.Configuration;
using Microsoft.Owin.Testing;
using Steeltoe.Management.Endpoint.Refresh;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointOwin.Test;
using System;
using System.Collections.Generic;
using System.Net;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.Refresh.Test
{
    public class RefreshEndpointOwinMiddlewareTest : BaseTest
    {
        public RefreshEndpointOwinMiddlewareTest()
        {
            ManagementOptions.Clear();
        }

        [Fact]
        public async void RefreshInvoke_ReturnsExpected()
        {
            // arrange
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(OwinTestHelpers.Appsettings);
            var opts = new RefreshEndpointOptions();
            var mopts = TestHelpers.GetManagementOptions(opts);
            var middle = new EndpointOwinMiddleware<IList<string>>(null, new RefreshEndpoint(opts, configurationBuilder.Build()), mopts);
            var context = OwinTestHelpers.CreateRequest("GET", "/cloudfoundryapplication/refresh");

            // act
            var json = await middle.InvokeAndReadResponse(context);

            // assert
            var expected = "[\"management\",\"management:endpoints\",\"management:endpoints:path\",\"management:endpoints:enabled\",\"Logging\",\"Logging:LogLevel\",\"Logging:LogLevel:Steeltoe\",\"Logging:LogLevel:Pivotal\",\"Logging:LogLevel:Default\",\"Logging:IncludeScopes\"]";
            Assert.Equal(expected, json);
        }

        [Fact]
        public async void RefreshHttpCall_ReturnsExpected()
        {
            var anc_env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            using (var server = TestServer.Create<Startup>())
            {
                var client = server.HttpClient;
                var result = await client.GetAsync("http://localhost/cloudfoundryapplication/refresh");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                var json = await result.Content.ReadAsStringAsync();

                // NOTE: removed these when copied from aspnetcore test... are they ANC only?
                // [from first position in 'expected':]\"urls\",
                // [from the end of 'expected':] ,\"environment\",\"applicationName\"
                var expected = "[\"management\",\"management:endpoints\",\"management:endpoints:path\",\"management:endpoints:enabled\",\"Logging\",\"Logging:LogLevel\",\"Logging:LogLevel:Steeltoe\",\"Logging:LogLevel:Pivotal\",\"Logging:LogLevel:Default\",\"Logging:IncludeScopes\"]";
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
            configurationBuilder.AddInMemoryCollection(OwinTestHelpers.Appsettings);
            var config = configurationBuilder.Build();
            var ep = new RefreshEndpoint(opts, config);
            var middle = new EndpointOwinMiddleware<IList<string>>(null, ep, mopts);

            Assert.True(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/refresh"));
            Assert.False(middle.RequestVerbAndPathMatch("PUT", "/cloudfoundryapplication/refresh"));
            Assert.False(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/badpath"));
        }
    }
}
