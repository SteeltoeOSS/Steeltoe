// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Endpoint.Loggers.Test
{
    public class EndpointMiddlewareTest : BaseTest
    {
        private static readonly Dictionary<string, string> AppSettings = new ()
        {
            ["Logging:IncludeScopes"] = "false",
            ["Logging:LogLevel:Default"] = "Warning",
            ["Logging:LogLevel:Pivotal"] = "Information",
            ["Logging:LogLevel:Steeltoe"] = "Information",
            ["management:endpoints:enabled"] = "true",
            ["management:endpoints:loggers:enabled"] = "true"
        };

        [Fact]
        public async Task HandleLoggersRequestAsync_ReturnsExpected()
        {
            var opts = new LoggersEndpointOptions();
            var mopts = new ActuatorManagementOptions();
            mopts.EndpointOptions.Add(opts);
            var ep = new TestLoggersEndpoint(opts);
            var middle = new LoggersEndpointMiddleware(null, ep, mopts);
            var context = CreateRequest("GET", "/loggers");
            await middle.HandleLoggersRequestAsync(context);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var rdr = new StreamReader(context.Response.Body);
            var json = await rdr.ReadToEndAsync();
            Assert.Equal("{}", json);
        }

        [Fact]
        public async Task LoggersActuator_ReturnsExpectedData()
        {
            var builder = new WebHostBuilder()
               .UseStartup<Startup>()
               .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(AppSettings))
               .ConfigureLogging((context, loggingBuilder) =>
               {
                   loggingBuilder.AddConfiguration(context.Configuration);
                   loggingBuilder.AddDynamicConsole();
               });

            using var server = new TestServer(builder);
            var client = server.CreateClient();
            var result = await client.GetFromJsonAsync<JsonElement>("http://localhost/cloudfoundryapplication/loggers");

            Assert.True(result.TryGetProperty("loggers", out var loggers));
            Assert.True(result.TryGetProperty("levels", out _));
            Assert.Equal("WARN", loggers.GetProperty("Default").GetProperty("configuredLevel").GetString());
            Assert.Equal("INFO", loggers.GetProperty("Steeltoe.Management").GetProperty("effectiveLevel").GetString());
        }

        [Fact]
        public async Task LoggersActuator_ReturnsBadRequest()
        {
            var builder = new WebHostBuilder()
               .UseStartup<Startup>()
               .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(AppSettings))
               .ConfigureLogging((context, loggingBuilder) =>
               {
                   loggingBuilder.AddConfiguration(context.Configuration);
                   loggingBuilder.AddDynamicConsole();
               });

            using var server = new TestServer(builder);
            var client = server.CreateClient();
            HttpContent content = new StringContent("{\"configuredLevel\":\"BadData\"}");
            var changeResult = await client.PostAsync("http://localhost/cloudfoundryapplication/loggers/Default", content);
            Assert.Equal(HttpStatusCode.BadRequest, changeResult.StatusCode);
        }

        [Fact]
        public async Task LoggersActuator_AcceptsPost()
        {
            var builder = new WebHostBuilder()
               .UseStartup<Startup>()
               .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(AppSettings))
               .ConfigureLogging((context, loggingBuilder) =>
               {
                   loggingBuilder.AddConfiguration(context.Configuration.GetSection("Logging"));
                   loggingBuilder.AddDynamicConsole();
               });

            using var server = new TestServer(builder);
            var client = server.CreateClient();
            HttpContent content = new StringContent("{\"configuredLevel\":\"ERROR\"}");
            var changeResult = await client.PostAsync("http://localhost/cloudfoundryapplication/loggers/Default", content);
            Assert.Equal(HttpStatusCode.OK, changeResult.StatusCode);

            var parsedObject = await client.GetFromJsonAsync<JsonElement>("http://localhost/cloudfoundryapplication/loggers");
            Assert.Equal("ERROR", parsedObject.GetProperty("loggers").GetProperty("Default").GetProperty("effectiveLevel").GetString());
        }

        [Fact]
        public async Task LoggersActuator_AcceptsPost_When_ManagementPath_Is_Slash()
        {
            var appSettings = new Dictionary<string, string>(AppSettings)
            {
                ["management:endpoints:path"] = "/"
            };
            appSettings.Add("Management:Endpoints:Actuator:Exposure:Include:0", "*");

            var builder = new WebHostBuilder()
               .UseStartup<Startup>()
               .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(appSettings))
               .ConfigureLogging((context, loggingBuilder) =>
               {
                   loggingBuilder.AddConfiguration(context.Configuration.GetSection("Logging"));
                   loggingBuilder.AddDynamicConsole();
               });

            using var server = new TestServer(builder);
            var client = server.CreateClient();
            HttpContent content = new StringContent("{\"configuredLevel\":\"ERROR\"}");
            var changeResult = await client.PostAsync("http://localhost/loggers/Default", content);
            Assert.Equal(HttpStatusCode.OK, changeResult.StatusCode);

            var parsedObject = await client.GetFromJsonAsync<JsonElement>("http://localhost/loggers");
            Assert.Equal("ERROR", parsedObject.GetProperty("loggers").GetProperty("Default").GetProperty("effectiveLevel").GetString());
        }

        [Fact]
        public async Task LoggersActuator_UpdateNameSpace_UpdatesChildren()
        {
            var builder = new WebHostBuilder()
               .UseStartup<Startup>()
               .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(AppSettings))
               .ConfigureLogging((context, loggingBuilder) =>
               {
                   loggingBuilder.AddConfiguration(context.Configuration.GetSection("Logging"));
                   loggingBuilder.AddDynamicConsole();
               });

            using var server = new TestServer(builder);
            var client = server.CreateClient();
            HttpContent content = new StringContent("{\"configuredLevel\":\"TRACE\"}");
            var changeResult = await client.PostAsync("http://localhost/cloudfoundryapplication/loggers/Steeltoe", content);
            Assert.Equal(HttpStatusCode.OK, changeResult.StatusCode);

            var json = await client.GetFromJsonAsync<JsonElement>("http://localhost/cloudfoundryapplication/loggers");
            var loggers = json.GetProperty("loggers");
            Assert.Equal("TRACE", loggers.GetProperty("Steeltoe").GetProperty("effectiveLevel").GetString());
            Assert.Equal("TRACE", loggers.GetProperty("Steeltoe.Management").GetProperty("effectiveLevel").GetString());
            Assert.Equal("TRACE", loggers.GetProperty("Steeltoe.Management.Endpoint").GetProperty("effectiveLevel").GetString());
            Assert.Equal("TRACE", loggers.GetProperty("Steeltoe.Management.Endpoint.Loggers").GetProperty("effectiveLevel").GetString());
            Assert.Equal("TRACE", loggers.GetProperty("Steeltoe.Management.Endpoint.Loggers.LoggersEndpointMiddleware").GetProperty("effectiveLevel").GetString());
        }

        [Fact]
        public void RoutesByPathAndVerb()
        {
            var options = new LoggersEndpointOptions();
            Assert.False(options.ExactMatch);
            Assert.Equal("/actuator/loggers/{**_}", options.GetContextPath(new ActuatorManagementOptions()));
            Assert.Equal("/cloudfoundryapplication/loggers/{**_}", options.GetContextPath(new CloudFoundryManagementOptions()));
            Assert.Collection(options.AllowedVerbs, verb => Assert.Contains("Get", verb), verb => Assert.Contains("Post", verb));
        }

        [Fact]
        public async Task LoggersActuator_MultipleProviders_ReturnsExpectedData()
        {
            var builder = new WebHostBuilder()
               .UseStartup<Startup>()
               .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(AppSettings))
               .ConfigureLogging((context, loggingBuilder) =>
               {
                   loggingBuilder.AddConfiguration(context.Configuration);
                   loggingBuilder.AddDynamicConsole();
                   loggingBuilder.AddDebug();
               });

            using var server = new TestServer(builder);
            var client = server.CreateClient();
            var result = await client.GetFromJsonAsync<JsonElement>("http://localhost/cloudfoundryapplication/loggers");
            Assert.True(result.TryGetProperty("loggers", out var loggers));
            Assert.True(result.TryGetProperty("levels", out _));
            Assert.Equal("WARN", loggers.GetProperty("Default").GetProperty("configuredLevel").GetString());
            Assert.Equal("INFO", loggers.GetProperty("Steeltoe.Management").GetProperty("effectiveLevel").GetString());
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
