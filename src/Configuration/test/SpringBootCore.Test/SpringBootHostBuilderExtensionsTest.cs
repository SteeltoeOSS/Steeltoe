// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using Xunit;

namespace Steeltoe.Extensions.Configuration.SpringBoot.Test;

public class SpringBootHostBuilderExtensionsTest
{
    [Fact]
    public void ConfigureSpringBoot_ThrowsIfNulls()
    {
        const IHostBuilder builder = null;
        const IWebHostBuilder webHostBuilder = null;

        Assert.Throws<ArgumentNullException>(() => builder.AddSpringBootConfiguration());
        Assert.Throws<ArgumentNullException>(() => webHostBuilder.AddSpringBootConfiguration());
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
        var hostBuilder = WebHost.CreateDefaultBuilder(new[] { "Spring.Cloud.Stream.Bindings.Input.Destination=testDestination", "Spring.Cloud.Stream.Bindings.Input.Group=testGroup" })
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
        var hostBuilder = Host.CreateDefaultBuilder(new[] { "Spring.Cloud.Stream.Bindings.Input.Destination=testDestination", "Spring.Cloud.Stream.Bindings.Input.Group=testGroup" })
            .AddSpringBootConfiguration();

        using var host = hostBuilder.Build();
        var config = host.Services.GetServices<IConfiguration>().SingleOrDefault();

        Assert.NotNull(config["spring:cloud:stream:bindings:input:destination"]);
        Assert.Equal("testDestination", config["spring:cloud:stream:bindings:input:destination"]);

        Assert.NotNull(config["spring:cloud:stream:bindings:input:group"]);
        Assert.Equal("testGroup", config["spring:cloud:stream:bindings:input:group"]);
    }

    [Fact]
    public void ConfigureSpringBoot_ThrowsIfNulls_Net60()
    {
        const WebApplicationBuilder webAppBuilder = null;

        Assert.Throws<ArgumentNullException>(() => webAppBuilder.AddSpringBootConfiguration());
    }

    [Fact]
    public void WebApplicationConfiguresIConfiguration_Spring_Application_Json()
    {
        Environment.SetEnvironmentVariable("SPRING_APPLICATION_JSON", "{\"foo.bar\":\"value\"}");
        var hostBuilder = TestHelpers.GetTestWebApplicationBuilder();

        hostBuilder.AddSpringBootConfiguration();
        var host = hostBuilder.Build();

        var config = host.Services.GetService<IConfiguration>();

        Assert.NotNull(config["foo:bar"]);
        Assert.Equal("value", config["foo:bar"]);

        Environment.SetEnvironmentVariable("SPRING_APPLICATION_JSON", string.Empty);
    }

    [Fact]
    public void WebApplicationConfiguresIConfiguration_CmdLine()
    {
        var hostBuilder = TestHelpers.GetTestWebApplicationBuilder(new[] { "Spring.Cloud.Stream.Bindings.Input.Destination=testDestination", "Spring.Cloud.Stream.Bindings.Input.Group=testGroup" });
        hostBuilder.AddSpringBootConfiguration();

        using var host = hostBuilder.Build();
        var config = host.Services.GetService<IConfiguration>();

        Assert.NotNull(config["spring:cloud:stream:bindings:input:destination"]);
        Assert.Equal("testDestination", config["spring:cloud:stream:bindings:input:destination"]);

        Assert.NotNull(config["spring:cloud:stream:bindings:input:group"]);
        Assert.Equal("testGroup", config["spring:cloud:stream:bindings:input:group"]);
    }
}
