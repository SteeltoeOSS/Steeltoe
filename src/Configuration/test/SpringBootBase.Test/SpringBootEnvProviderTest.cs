// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using System;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Extensions.Configuration.SpringBoot.Test
{
    public class SpringBootEnvProviderTest
    {
        [Fact]
        public void TryGet_Flat()
        {
            var prov = new SpringBootEnvProvider()
            {
                SpringApplicationJson = "{\"management.metrics.tags.application.type\":\"${spring.cloud.dataflow.stream.app.type:unknown}\"}"
            };
            prov.Load();
            prov.TryGet("management:metrics:tags:application:type", out var value);
            Assert.NotNull(value);
            Assert.Equal("${spring.cloud.dataflow.stream.app.type:unknown}", value);
        }

        [Fact]
        public void TryGet_Tree()
        {
            var prov = new SpringBootEnvProvider()
            {
                SpringApplicationJson =
                    @"{
                        ""a.b.c"": {
                                     ""e.f"": {
                                                ""g"":""h"",
                                                ""i.j"":""k""
                                              }
                                   },
                        ""l.m.n"": ""o"",
                        ""p"": null
                    }"
            };

            prov.Load();
            prov.TryGet("a:b:c:e:f:i:j", out var value);
            Assert.NotNull(value);
            Assert.Equal("k", value);
            prov.TryGet("l:m:n", out value);
            Assert.NotNull(value);
            Assert.Equal("o", value);
        }

        [Fact]
        public void TryGet_Malformed()
        {
            var testOutputHelper = new TestOutputHelper();

            var prov = new SpringBootEnvProvider(new XunitLoggerFactory(testOutputHelper))
            {
                SpringApplicationJson =
                    @"{""test\"":}"
            };

            prov.Load();

            Assert.Contains("SPRING_APPLICATION_JSON", testOutputHelper.Output);
        }

        private class TestOutputHelper : ITestOutputHelper
        {
            private StringBuilder _stringBuilder = new StringBuilder();

            public string Output => _stringBuilder.ToString();

            public void WriteLine(string message) => _stringBuilder.Append(message);

            public void WriteLine(string format, params object[] args) => _stringBuilder.AppendFormat(format, args);
        }
    }
}
