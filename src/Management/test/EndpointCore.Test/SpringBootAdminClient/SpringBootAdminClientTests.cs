// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
