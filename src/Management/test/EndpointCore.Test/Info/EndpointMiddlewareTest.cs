// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Info.Contributor;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Xunit;

namespace Steeltoe.Management.Endpoint.Info.Test
{
    public class EndpointMiddlewareTest : BaseTest
    {
        private readonly Dictionary<string, string> appSettings = new Dictionary<string, string>()
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/management",
            ["management:endpoints:info:enabled"] = "true",
            ["management:endpoints:info:id"] = "infomanagement",
            ["management:endpoints:actuator:exposure:include:0"] = "*",
            ["info:application:name"] = "foobar",
            ["info:application:version"] = "1.0.0'",
            ["info:application:date"] = "5/1/2008",
            ["info:application:time"] = "8:30:52 AM",
            ["info:NET:type"] = "Core",
            ["info:NET:version"] = "2.0.0",
            ["info:NET:ASPNET:type"] = "Core",
            ["info:NET:ASPNET:version"] = "2.0.0"
        };

        [Fact]
        public async void HandleInfoRequestAsync_ReturnsExpected()
        {
            var opts = new InfoEndpointOptions();
            var mopts = new ActuatorManagementOptions();
            mopts.EndpointOptions.Add(opts);
            var contribs = new List<IInfoContributor>() { new GitInfoContributor() };
            var ep = new TestInfoEndpoint(opts, contribs);
            var middle = new InfoEndpointMiddleware(null, ep, mopts);
            var context = CreateRequest("GET", "/loggers");
            await middle.HandleInfoRequestAsync(context);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            StreamReader rdr = new StreamReader(context.Response.Body);
            string json = await rdr.ReadToEndAsync();
            Assert.Equal("{}", json);
        }

        [Fact]
        public async void InfoActuator_ReturnsExpectedData()
        {
            // Note: This test pulls in from git.properties and appsettings created
            // in the Startup class
            var builder = new WebHostBuilder()
                .UseStartup<Startup>()
                .ConfigureAppConfiguration((context, config) => config.AddInMemoryCollection(appSettings));

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/management/infomanagement");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                var json = await result.Content.ReadAsStringAsync();
                Assert.NotNull(json);
                var dict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(json);
                Assert.NotNull(dict);

                Assert.Equal(3, dict.Count);
                Assert.True(dict.ContainsKey("application"));
                Assert.True(dict.ContainsKey("NET"));
                Assert.True(dict.ContainsKey("git"));

                var appNode = dict["application"] as Dictionary<string, object>;
                Assert.NotNull(appNode);
                Assert.Equal("foobar", appNode["name"]);

                var netNode = dict["NET"] as Dictionary<string, object>;
                Assert.NotNull(netNode);
                Assert.Equal("Core", netNode["type"]);

                var gitNode = dict["git"] as Dictionary<string, object>;
                Assert.NotNull(gitNode);
                Assert.True(gitNode.ContainsKey("build"));
                Assert.True(gitNode.ContainsKey("branch"));
                Assert.True(gitNode.ContainsKey("commit"));
                Assert.True(gitNode.ContainsKey("closest"));
                Assert.True(gitNode.ContainsKey("dirty"));
                Assert.True(gitNode.ContainsKey("remote"));
                Assert.True(gitNode.ContainsKey("tags"));
            }
        }

        //[Fact]
        //public void InfoEndpointMiddleware_PathAndVerbMatching_ReturnsExpected()
        //{
        //    var opts = new InfoEndpointOptions();
        //    var mopts = TestHelper.GetManagementOptions(opts);
        //    var contribs = new List<IInfoContributor>() { new GitInfoContributor() };
        //    var ep = new InfoEndpoint(opts, contribs);
        //    var middle = new InfoEndpointMiddleware(null, ep, mopts);

        //    Assert.True(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/info"));
        //    Assert.False(middle.RequestVerbAndPathMatch("PUT", "/cloudfoundryapplication/info"));
        //    Assert.False(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/badpath"));
        //}

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
