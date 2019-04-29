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

using Microsoft.Owin.Testing;
using Newtonsoft.Json;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointOwin.Test;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.Loggers.Test
{
    public class LoggersEndpointOwinMiddlewareTest : BaseTest
    {
        public LoggersEndpointOwinMiddlewareTest()
        {
            ManagementOptions.Clear();
        }

        [Fact]
        public async void LoggersInvoke_ReturnsExpected()
        {
            var opts = new LoggersEndpointOptions();
            var mopts = TestHelpers.GetManagementOptions(opts);
            var ep = new TestLoggersEndpoint(opts);
            var middle = new LoggersEndpointOwinMiddleware(null, ep, mopts);
            var context = OwinTestHelpers.CreateRequest("GET", "/cloudfoundryapplication/loggers");
            await middle.Invoke(context);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            StreamReader rdr = new StreamReader(context.Response.Body);
            string json = await rdr.ReadToEndAsync();
            Assert.Equal("{}", json);
        }

        [Fact]
        public async void LoggersHttpGet_ReturnsExpected()
        {
            using (var server = TestServer.Create<Startup>())
            {
                var client = server.HttpClient;
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
        }

        [Fact]
        public async void LoggersHttpPost_ReturnsExpected()
        {
            using (var server = TestServer.Create<Startup>())
            {
                var client = server.HttpClient;
                HttpContent content = new StringContent("{\"configuredLevel\":\"ERROR\"}");
                var changeResult = await client.PostAsync("http://localhost/cloudfoundryapplication/loggers/Default", content);
                Assert.Equal(HttpStatusCode.OK, changeResult.StatusCode);

                var validationResult = await client.GetAsync("http://localhost/cloudfoundryapplication/loggers");
                var json = await validationResult.Content.ReadAsStringAsync();
                dynamic parsedObject = JsonConvert.DeserializeObject(json);
                Assert.Equal("ERROR", parsedObject.loggers.Default.effectiveLevel.ToString());
            }
        }

        [Fact]
        public async void LoggersHttpPost_UpdateNameSpace_UpdatesChildren()
        {
            using (var server = TestServer.Create<Startup>())
            {
                var client = server.HttpClient;
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
                Assert.Equal("TRACE", parsedObject.loggers["Steeltoe.Management.Endpoint.Loggers.LoggersEndpoint"].effectiveLevel.ToString());
            }
        }

        [Fact]
        public void LoggersEndpointMiddleware_PathAndVerbMatching_ReturnsExpected()
        {
            var opts = new LoggersEndpointOptions();
            var mopts = TestHelpers.GetManagementOptions(opts);
            var ep = new LoggersEndpoint(opts, (IDynamicLoggerProvider)null);
            var middle = new LoggersEndpointOwinMiddleware(null, ep, mopts);

            Assert.True(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/loggers"));
            Assert.False(middle.RequestVerbAndPathMatch("PUT", "/cloudfoundryapplication/loggers"));
            Assert.False(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/badpath"));
            Assert.True(middle.RequestVerbAndPathMatch("POST", "/cloudfoundryapplication/loggers"));
            Assert.False(middle.RequestVerbAndPathMatch("POST", "/cloudfoundryapplication/badpath"));
            Assert.True(middle.RequestVerbAndPathMatch("POST", "/cloudfoundryapplication/loggers/Foo.Bar.Class"));
            Assert.False(middle.RequestVerbAndPathMatch("POST", "/cloudfoundryapplication/badpath/Foo.Bar.Class"));
        }
    }
}
