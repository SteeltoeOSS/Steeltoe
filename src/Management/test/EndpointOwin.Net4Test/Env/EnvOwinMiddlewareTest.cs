// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            var configurationBuilder = new ConfigurationBuilder();
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
            var expected = "{\"activeProfiles\":[\"EnvironmentName\"],\"propertySources\":[{\"properties\":{\"Logging:IncludeScopes\":{\"value\":\"false\"},\"Logging:LogLevel:Default\":{\"value\":\"Warning\"},\"Logging:LogLevel:Pivotal\":{\"value\":\"Information\"},\"Logging:LogLevel:Steeltoe\":{\"value\":\"Information\"},\"management:endpoints:enabled\":{\"value\":\"true\"},\"management:endpoints:path\":{\"value\":\"/cloudfoundryapplication\"}},\"name\":\"MemoryConfigurationProvider\"}]}";
            Assert.Equal(expected, json);
        }

        [Fact]
        public async void EnvHttpCall_ReturnsExpected()
        {
            using var server = TestServer.Create<Startup>();
            var client = server.HttpClient;
            var result = await client.GetAsync("http://localhost/cloudfoundryapplication/env");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var json = await result.Content.ReadAsStringAsync();

            // REVIEW: ChainedConfigurationProvider with Application Name isn't coming back
            // "{\"activeProfiles\":[\"Production\"],\"propertySources\":[{\"properties\":{\"applicationName\":{\"value\":\"Steeltoe.Management.EndpointOwin.Test\"}},\"name\":\"ChainedConfigurationProvider\"},{\"properties\":{\"Logging:IncludeScopes\":{\"value\":\"false\"},\"Logging:LogLevel:Default\":{\"value\":\"Warning\"},\"Logging:LogLevel:Pivotal\":{\"value\":\"Information\"},\"Logging:LogLevel:Steeltoe\":{\"value\":\"Information\"},\"management:endpoints:enabled\":{\"value\":\"true\"},\"management:endpoints:path\":{\"value\":\"/cloudfoundryapplication\"}},\"name\":\"MemoryConfigurationProvider\"}]}";
            var expected = "{\"activeProfiles\":[\"Production\"],\"propertySources\":[{\"properties\":{\"Logging:IncludeScopes\":{\"value\":\"false\"},\"Logging:LogLevel:Default\":{\"value\":\"Warning\"},\"Logging:LogLevel:Pivotal\":{\"value\":\"Information\"},\"Logging:LogLevel:Steeltoe\":{\"value\":\"Information\"},\"management:endpoints:enabled\":{\"value\":\"true\"},\"management:endpoints:path\":{\"value\":\"/cloudfoundryapplication\"}},\"name\":\"MemoryConfigurationProvider\"}]}";
            Assert.Equal(expected, json);
        }

        [Fact]
        public void EnvEndpointMiddleware_PathAndVerbMatching_ReturnsExpected()
        {
            var opts = new EnvEndpointOptions();
            var configurationBuilder = new ConfigurationBuilder();
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
