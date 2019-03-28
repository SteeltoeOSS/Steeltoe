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
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Env;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointOwin.Test;
using System.Collections.Generic;
using System.Net;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.Env.Test
{
    public class EnvOwinMiddlewareTest : BaseTest
    {
        public EnvOwinMiddlewareTest()
        {
            ManagementOptions.Clear();
        }

        [Fact]
        public async void EnvInvoke_ReturnsExpected()
        {
            // arrange
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(OwinTestHelpers.Appsettings);
            var config = configurationBuilder.Build();
            var ep = new EnvEndpoint(new EnvEndpointOptions(), config, new GenericHostingEnvironment() { EnvironmentName = "EnvironmentName" });
            var mgmt = new CloudFoundryManagementOptions()
            {
                Path = "/"
            };
            var middle = new EndpointOwinMiddleware<EnvironmentDescriptor>(null, ep, new List<IManagementOptions> { mgmt });
            var context = OwinTestHelpers.CreateRequest("GET", "/env");

            // act
            var json = await middle.InvokeAndReadResponse(context);

            // assert
            var expected = "{\"activeProfiles\":[\"EnvironmentName\"],\"propertySources\":[{\"properties\":{\"management:endpoints:path\":{\"value\":\"/cloudfoundryapplication\"},\"management:endpoints:enabled\":{\"value\":\"true\"},\"Logging:LogLevel:Steeltoe\":{\"value\":\"Information\"},\"Logging:LogLevel:Pivotal\":{\"value\":\"Information\"},\"Logging:LogLevel:Default\":{\"value\":\"Warning\"},\"Logging:IncludeScopes\":{\"value\":\"false\"}},\"name\":\"MemoryConfigurationProvider\"}]}";
            Assert.Equal(expected, json);
        }

        [Fact]
        public async void EnvHttpCall_ReturnsExpected()
        {
            using (var server = TestServer.Create<Startup>())
            {
                var client = server.HttpClient;
                var result = await client.GetAsync("http://localhost/cloudfoundryapplication/env");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                var json = await result.Content.ReadAsStringAsync();

                // REVIEW: ChainedConfigurationProvider with Application Name isn't coming back
                // "{\"activeProfiles\":[\"Production\"],\"propertySources\":[{\"properties\":{\"applicationName\":{\"value\":\"Steeltoe.Management.EndpointOwin.Test\"}},\"name\":\"ChainedConfigurationProvider\"},{\"properties\":{\"management:endpoints:sensitive\":{\"value\":\"false\"},\"management:endpoints:path\":{\"value\":\"/cloudfoundryapplication\"},\"management:endpoints:enabled\":{\"value\":\"true\"},\"Logging:LogLevel:Steeltoe\":{\"value\":\"Information\"},\"Logging:LogLevel:Pivotal\":{\"value\":\"Information\"},\"Logging:LogLevel:Default\":{\"value\":\"Warning\"},\"Logging:IncludeScopes\":{\"value\":\"false\"}},\"name\":\"MemoryConfigurationProvider\"}]}";
                var expected = "{\"activeProfiles\":[\"Production\"],\"propertySources\":[{\"properties\":{\"management:endpoints:path\":{\"value\":\"/cloudfoundryapplication\"},\"management:endpoints:enabled\":{\"value\":\"true\"},\"Logging:LogLevel:Steeltoe\":{\"value\":\"Information\"},\"Logging:LogLevel:Pivotal\":{\"value\":\"Information\"},\"Logging:LogLevel:Default\":{\"value\":\"Warning\"},\"Logging:IncludeScopes\":{\"value\":\"false\"}},\"name\":\"MemoryConfigurationProvider\"}]}";
                Assert.Equal(expected, json);
            }
        }

        [Fact]
        public void EnvEndpointMiddleware_PathAndVerbMatching_ReturnsExpected()
        {
            var opts = new EnvEndpointOptions();
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            var config = configurationBuilder.Build();
            var host = new GenericHostingEnvironment() { EnvironmentName = "EnvironmentName" };
            var ep = new EnvEndpoint(opts, config, host);
            var mgmt = new CloudFoundryManagementOptions()
            {
                Path = "/"
            };
            var middle = new EndpointOwinMiddleware<EnvironmentDescriptor>(null, ep, new List<IManagementOptions> { mgmt });

            Assert.True(middle.RequestVerbAndPathMatch("GET", "/env"));
            Assert.False(middle.RequestVerbAndPathMatch("PUT", "/env"));
            Assert.False(middle.RequestVerbAndPathMatch("GET", "/badpath"));
        }
    }
}
