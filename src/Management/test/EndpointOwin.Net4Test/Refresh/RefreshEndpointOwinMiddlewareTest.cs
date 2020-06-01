// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            var mopts = TestHelper.GetManagementOptions(opts);
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
            var mopts = TestHelper.GetManagementOptions(opts);
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
