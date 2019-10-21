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

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Steeltoe.Extensions.Configuration.PlaceholderCore.Test
{
    public class PlaceholderServiceCollectionExtensionsTest
    {
        [Fact]
        public void ConfigurePlaceholderResolver_ThrowsIfNulls()
        {
            // Arrange
            IServiceCollection services = null;
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => PlaceholderResolverExtensions.ConfigurePlaceholderResolver(services, config));
            ex = Assert.Throws<ArgumentNullException>(() => PlaceholderResolverExtensions.ConfigurePlaceholderResolver(new ServiceCollection(), config));
        }

        [Fact]
        public void ConfigurePlaceholderResolver_ConfiguresIConfiguration_ReplacesExisting()
        {
            // Arrange
            Dictionary<string, string> settings = new Dictionary<string, string>()
            {
                { "key1", "value1" },
                { "key2", "${key1?notfound}" },
                { "key3", "${nokey?notfound}" },
                { "key4", "${nokey}" },
            };
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(settings);
            var config1 = builder.Build();

            var hostBuilder = new WebHostBuilder()
                       .UseStartup<TestServerStartup>()
                       .UseConfiguration(config1);

            using (var server = new TestServer(hostBuilder))
            {
                var services = TestServerStartup.ServiceProvider;
                var config2 = services.GetServices<IConfiguration>().SingleOrDefault();
                Assert.NotSame(config1, config2);

                Assert.Null(config2["nokey"]);
                Assert.Equal("value1", config2["key1"]);
                Assert.Equal("value1", config2["key2"]);
                Assert.Equal("notfound", config2["key3"]);
                Assert.Equal("${nokey}", config2["key4"]);
            }
        }

        [Fact]
        public void AddPlaceholderResolver_HostBuilder_WrapsApplicationsConfiguration()
        {
            var appsettingsJson = @"
                {
                    ""spring"": {
                        ""json"": {
                            ""name"": ""myName""
                    },
                      ""cloud"": {
                        ""config"": {
                            ""name"" : ""${spring:xml:name?noname}"",
                        }
                      }
                    }
                }";

            var appsettingsXml = @"
                <settings>
                    <spring>
                        <xml>
                            <name>${spring:ini:name?noName}</name>
                        </xml>
                    </spring>
                </settings>";

            var appsettingsIni = @"
[spring:ini]
    name=${spring:line:name?noName}
";
            var appsettingsLine = new string[]
            {
                "--spring:line:name=${spring:json:name?noName}"
            };
            var jsonpath = TestHelpers.CreateTempFile(appsettingsJson);
            string jsonfileName = Path.GetFileName(jsonpath);
            var xmlpath = TestHelpers.CreateTempFile(appsettingsXml);
            string xmlfileName = Path.GetFileName(xmlpath);
            var inipath = TestHelpers.CreateTempFile(appsettingsIni);
            string inifileName = Path.GetFileName(inipath);

            string directory = Path.GetDirectoryName(jsonpath);

            var hostBuilder = new WebHostBuilder()
             .UseStartup<TestServerStartup1>()
             .ConfigureAppConfiguration((configurationBuilder) =>
             {
                 configurationBuilder.SetBasePath(directory);
                 configurationBuilder.AddJsonFile(jsonfileName);
                 configurationBuilder.AddXmlFile(xmlfileName);
                 configurationBuilder.AddIniFile(inifileName);
                 configurationBuilder.AddCommandLine(appsettingsLine);
             })
             .AddPlaceholderResolver();

            using (var server = new TestServer(hostBuilder))
            {
                var services = TestServerStartup1.ServiceProvider;
                var config = services.GetServices<IConfiguration>().SingleOrDefault();
                Assert.Equal("myName", config["spring:cloud:config:name"]);
            }
        }
    }
}
