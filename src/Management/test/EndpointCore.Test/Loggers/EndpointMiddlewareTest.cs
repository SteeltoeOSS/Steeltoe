// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using Xunit;

namespace Steeltoe.Management.Endpoint.Loggers.Test
{
    public class EndpointMiddlewareTest : BaseTest
    {
        private static readonly Dictionary<string, string> AppSettings = new Dictionary<string, string>()
        {
            ["Logging:IncludeScopes"] = "false",
            ["Logging:LogLevel:Default"] = "Warning",
            ["Logging:LogLevel:Pivotal"] = "Information",
            ["Logging:LogLevel:Steeltoe"] = "Information",
            ["management:endpoints:enabled"] = "true",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:loggers:enabled"] = "true"
        };

        [Fact]
        public async void HandleLoggersRequestAsync_ReturnsExpected()
        {
            var opts = new LoggersEndpointOptions();
            var mopts = TestHelper.GetManagementOptions(opts);
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
        public async void LoggersActuator_ReturnsExpectedData()
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
            var result = await client.GetAsync("http://localhost/cloudfoundryapplication/loggers");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var json = await result.Content.ReadAsStringAsync();
            Assert.NotNull(json);

            var loggers = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.NotNull(loggers);
            Assert.True(loggers.ContainsKey("levels"));
            Assert.True(loggers.ContainsKey("loggers"));

            // at least one logger should be returned
            Assert.True(loggers["loggers"].ToString().Length > 2);

            // parse the response into a dynamic object, verify that Default was returned and configured at Warning
            dynamic parsedObject = JsonConvert.DeserializeObject(json);
            Assert.Equal("WARN", parsedObject.loggers.Default.configuredLevel.ToString());
        }

        [Fact]
        public async void LoggersActuator_AcceptsPost()
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

            var validationResult = await client.GetAsync("http://localhost/cloudfoundryapplication/loggers");
            var json = await validationResult.Content.ReadAsStringAsync();
            dynamic parsedObject = JsonConvert.DeserializeObject(json);
            Assert.Equal("ERROR", parsedObject.loggers.Default.effectiveLevel.ToString());
        }

        [Fact]
        public async void LoggersActuator_AcceptsPost_When_ManagementPath_Is_Slash()
        {
            var appSettings = new Dictionary<string, string>(AppSettings);
            appSettings["management:endpoints:path"] = "/";
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

            var validationResult = await client.GetAsync("http://localhost/loggers");
            var json = await validationResult.Content.ReadAsStringAsync();
            dynamic parsedObject = JsonConvert.DeserializeObject(json);
            Assert.Equal("ERROR", parsedObject.loggers.Default.effectiveLevel.ToString());
        }

        [Fact]
        public async void LoggersActuator_UpdateNameSpace_UpdatesChildren()
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

            var validationResult = await client.GetAsync("http://localhost/cloudfoundryapplication/loggers");
            var json = await validationResult.Content.ReadAsStringAsync();
            dynamic parsedObject = JsonConvert.DeserializeObject(json);
            Assert.Equal("TRACE", parsedObject.loggers.Steeltoe.effectiveLevel.ToString());
            Assert.Equal("TRACE", parsedObject.loggers["Steeltoe.Management"].effectiveLevel.ToString());
            Assert.Equal("TRACE", parsedObject.loggers["Steeltoe.Management.Endpoint"].effectiveLevel.ToString());
            Assert.Equal("TRACE", parsedObject.loggers["Steeltoe.Management.Endpoint.Loggers"].effectiveLevel.ToString());
            Assert.Equal("TRACE", parsedObject.loggers["Steeltoe.Management.Endpoint.Loggers.LoggersEndpointMiddleware"].effectiveLevel.ToString());
        }

        [Fact]
        public void LoggersEndpointMiddleware_PathAndVerbMatching_ReturnsExpected()
        {
            var opts = new LoggersEndpointOptions();
            var mopts = TestHelper.GetManagementOptions(opts);
            var ep = new LoggersEndpoint(opts, null);
            var middle = new LoggersEndpointMiddleware(null, ep, mopts);

            Assert.True(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/loggers"));
            Assert.False(middle.RequestVerbAndPathMatch("PUT", "/cloudfoundryapplication/loggers"));
            Assert.False(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/badpath"));
            Assert.True(middle.RequestVerbAndPathMatch("POST", "/cloudfoundryapplication/loggers"));
            Assert.False(middle.RequestVerbAndPathMatch("POST", "/cloudfoundryapplication/badpath"));
            Assert.True(middle.RequestVerbAndPathMatch("POST", "/cloudfoundryapplication/loggers/Foo.Bar.Class"));
            Assert.False(middle.RequestVerbAndPathMatch("POST", "/cloudfoundryapplication/badpath/Foo.Bar.Class"));
        }

        [Fact]
        public async void LoggersActuator_MultipleProviders_ReturnsExpectedData()
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
            var result = await client.GetAsync("http://localhost/cloudfoundryapplication/loggers");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var json = await result.Content.ReadAsStringAsync();
            Assert.NotNull(json);

            var loggers = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.NotNull(loggers);
            Assert.True(loggers.ContainsKey("levels"));
            Assert.True(loggers.ContainsKey("loggers"));

            // at least one logger should be returned
            Assert.True(loggers["loggers"].ToString().Length > 2);

            // parse the response into a dynamic object, verify that Default was returned and configured at Warning
            dynamic parsedObject = JsonConvert.DeserializeObject(json);
            Assert.Equal("WARN", parsedObject.loggers.Default.configuredLevel.ToString());
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
