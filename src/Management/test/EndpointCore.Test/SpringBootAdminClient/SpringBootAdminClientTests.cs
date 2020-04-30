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
using Steeltoe.Management.Endpoint.SpringBootAdminClient;
using System;
using System.Collections.Generic;
using System.Text;

using Xunit;

namespace Steeltoe.Management.EndpointCore.Test.SpringBootAdminClient
{
    public class SpringBootAdminClientTests
    {
        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            IConfiguration config = null;
            Assert.Throws<ArgumentNullException>(() => new BootAdminClientOptions(config));
        }

        [Fact]
        public void Constructor_InitializesWithEmptyConfiguration()
        {
            var appsettings = new Dictionary<string, string>()
            {
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var opts = new BootAdminClientOptions(config);
            Assert.NotNull(opts);
        }

        [Fact]
        public void Constructor_BindsConfiguration()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["management:endpoints:path"] = "/management",
                ["management:endpoints:health:path"] = "myhealth",
                ["URLS"] = "http://localhost:8080;https://localhost:8082",
                ["spring:boot:admin:client:url"] = "http://springbootadmin:9090",
                ["spring:application:name"] = "MySteeltoeApplication",
                ["ApplicationName"] = "OtherApplicationName"
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var opts = new BootAdminClientOptions(config);
            Assert.NotNull(opts);
            Assert.Equal("MySteeltoeApplication", opts.ApplicationName);
            Assert.Equal("http://localhost:8080", opts.BasePath);
            Assert.Equal("http://springbootadmin:9090", opts.Url);
        }

        [Fact]
        public void Constructor_BindsFallBack()
        {
            var appsettings = new Dictionary<string, string>();
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var opts = new BootAdminClientOptions(config);
            Assert.NotNull(opts);
            Assert.NotEmpty(opts.ApplicationName);
        }
    }
}
