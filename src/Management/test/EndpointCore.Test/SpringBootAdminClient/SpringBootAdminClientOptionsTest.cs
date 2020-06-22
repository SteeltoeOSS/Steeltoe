﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using System;
using System.Collections.Generic;

using Xunit;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient.Test
{
    public class SpringBootAdminClientOptionsTest
    {
        [Fact]
        public void Constructor_ThrowsOnNulls()
        {
            var ex1 = Assert.Throws<ArgumentNullException>(() => new SpringBootAdminClientOptions(null, new ApplicationInstanceInfo()));
            Assert.Equal("config", ex1.ParamName);
            var ex2 = Assert.Throws<ArgumentNullException>(() => new SpringBootAdminClientOptions(new ConfigurationBuilder().Build(), null));
            Assert.Equal("appInfo", ex2.ParamName);
        }

        [Fact]
        public void ConstructorFailsWithoutBaseAppUrl()
        {
            var ex = Assert.Throws<NullReferenceException>(() => new SpringBootAdminClientOptions(new ConfigurationBuilder().Build(), new ApplicationInstanceInfo()));
            Assert.Contains(":BasePath in order to register with Spring Boot Admin", ex.Message);
        }

        [Fact]
        public void ConstructorUsesAppInfo()
        {
            var appsettings = new Dictionary<string, string> { { "application:Uris:0", "http://somehost" } };
            var config = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
            var appInfo = new ApplicationInstanceInfo(config, string.Empty);

            var opts = new SpringBootAdminClientOptions(config, appInfo);

            Assert.NotNull(opts);
            Assert.Equal("http://somehost", opts.BasePath);
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
            var config = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();

            var opts = new SpringBootAdminClientOptions(config, new ApplicationInstanceInfo(config));

            Assert.NotNull(opts);
            Assert.Equal("MySteeltoeApplication", opts.ApplicationName);
            Assert.Equal("http://localhost:8080", opts.BasePath);
            Assert.Equal("http://springbootadmin:9090", opts.Url);
        }

        [Fact]
        public void Constructor_BindsFallBack()
        {
            var appsettings = new Dictionary<string, string> { { "spring:boot:admin:client:basepath", "http://somehost" } };
            var config = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();

            var opts = new SpringBootAdminClientOptions(config, new ApplicationInstanceInfo(config));

            Assert.NotNull(opts);
            Assert.NotEmpty(opts.ApplicationName);
        }
    }
}
