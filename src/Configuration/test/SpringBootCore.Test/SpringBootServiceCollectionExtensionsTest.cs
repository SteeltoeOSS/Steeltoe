﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;

using Xunit;

namespace Steeltoe.Extensions.Configuration.SpringBoot.Test
{
    public class SpringBootServiceCollectionExtensionsTest
    {
        [Fact]
        public void ConfigureSpringBoot_ThrowsIfNulls()
        {
            IHostBuilder builder = null;
            IWebHostBuilder webHostBuilder = null;

            var ex = Assert.Throws<ArgumentNullException>(() => SpringBootHostBuilderExtensions.AddSpringBootConfiguration(builder));
            ex = Assert.Throws<ArgumentNullException>(() => SpringBootHostBuilderExtensions.AddSpringBootConfiguration(webHostBuilder));
        }

        [Fact]
        public void WebHostConfiguresIConfiguration_Spring_Application_Json()
        {
            Environment.SetEnvironmentVariable("SPRING_APPLICATION_JSON", "{\"foo.bar\":\"value\"}");

            var hostBuilder = new WebHostBuilder()
                       .UseStartup<TestServerStartup>()
                       .AddSpringBootConfiguration();

            using var server = new TestServer(hostBuilder);
            var services = TestServerStartup.ServiceProvider;
            var config = services.GetServices<IConfiguration>().SingleOrDefault();

            Assert.NotNull(config["foo:bar"]);
            Assert.Equal("value", config["foo:bar"]);

            Environment.SetEnvironmentVariable("SPRING_APPLICATION_JSON", string.Empty);
        }

        [Fact]
        public void WebHostConfiguresIConfiguration_CmdLine()
        {
            var hostBuilder = WebHost.CreateDefaultBuilder(new string[] { "Spring.Cloud.Stream.Bindings.Input.Destination=testDestination", "Spring.Cloud.Stream.Bindings.Input.Group=testGroup" })
                       .UseStartup<TestServerStartup>()
                       .AddSpringBootConfiguration();

            using var server = new TestServer(hostBuilder);

            var services = TestServerStartup.ServiceProvider;
            var config = services.GetServices<IConfiguration>().SingleOrDefault();

            Assert.NotNull(config["spring:cloud:stream:bindings:input:destination"]);
            Assert.Equal("testDestination", config["spring:cloud:stream:bindings:input:destination"]);

            Assert.NotNull(config["spring:cloud:stream:bindings:input:group"]);
            Assert.Equal("testGroup", config["spring:cloud:stream:bindings:input:group"]);
        }

        [Fact]
        public void GenericHostConfiguresIConfiguration_Spring_Application_Json()
        {
            Environment.SetEnvironmentVariable("SPRING_APPLICATION_JSON", "{\"foo.bar\":\"value\"}");

            var hostBuilder = new HostBuilder()
                       .AddSpringBootConfiguration();
            var host = hostBuilder.Build();
            var config = host.Services.GetServices<IConfiguration>().SingleOrDefault();

            Assert.NotNull(config["foo:bar"]);
            Assert.Equal("value", config["foo:bar"]);

            Environment.SetEnvironmentVariable("SPRING_APPLICATION_JSON", string.Empty);
        }

        [Fact]
        public void GenericHostConfiguresIConfiguration_CmdLine()
        {
            var hostBuilder = Host.CreateDefaultBuilder(new string[] { "Spring.Cloud.Stream.Bindings.Input.Destination=testDestination", "Spring.Cloud.Stream.Bindings.Input.Group=testGroup" })
                       .AddSpringBootConfiguration();

            using var host = hostBuilder.Build();
            var config = host.Services.GetServices<IConfiguration>().SingleOrDefault();

            Assert.NotNull(config["spring:cloud:stream:bindings:input:destination"]);
            Assert.Equal("testDestination", config["spring:cloud:stream:bindings:input:destination"]);

            Assert.NotNull(config["spring:cloud:stream:bindings:input:group"]);
            Assert.Equal("testGroup", config["spring:cloud:stream:bindings:input:group"]);
        }
    }
}
