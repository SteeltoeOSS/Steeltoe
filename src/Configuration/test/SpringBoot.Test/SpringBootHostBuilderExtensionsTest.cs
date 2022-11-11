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
using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Configuration.SpringBoot.Test;

public sealed class SpringBootHostBuilderExtensionsTest
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

        IWebHostBuilder hostBuilder = new WebHostBuilder().UseStartup<TestServerStartup>().AddSpringBootConfiguration();

        using var server = new TestServer(hostBuilder);
        IServiceProvider services = TestServerStartup.ServiceProvider;
        IConfiguration configuration = services.GetServices<IConfiguration>().SingleOrDefault();

        Assert.NotNull(configuration["foo:bar"]);
        Assert.Equal("value", configuration["foo:bar"]);

        Environment.SetEnvironmentVariable("SPRING_APPLICATION_JSON", string.Empty);
    }

    [Fact]
    public void WebHostConfiguresIConfiguration_CmdLine()
    {
        IWebHostBuilder hostBuilder = WebHost.CreateDefaultBuilder(new[]
        {
            "Spring.Cloud.Stream.Bindings.Input.Destination=testDestination",
            "Spring.Cloud.Stream.Bindings.Input.Group=testGroup"
        }).UseStartup<TestServerStartup>().AddSpringBootConfiguration();

        using var server = new TestServer(hostBuilder);

        IServiceProvider services = TestServerStartup.ServiceProvider;
        IConfiguration configuration = services.GetServices<IConfiguration>().SingleOrDefault();

        Assert.NotNull(configuration["spring:cloud:stream:bindings:input:destination"]);
        Assert.Equal("testDestination", configuration["spring:cloud:stream:bindings:input:destination"]);

        Assert.NotNull(configuration["spring:cloud:stream:bindings:input:group"]);
        Assert.Equal("testGroup", configuration["spring:cloud:stream:bindings:input:group"]);
    }

    [Fact]
    public void GenericHostConfiguresIConfiguration_Spring_Application_Json()
    {
        Environment.SetEnvironmentVariable("SPRING_APPLICATION_JSON", "{\"foo.bar\":\"value\"}");

        IHostBuilder hostBuilder = new HostBuilder().AddSpringBootConfiguration();
        IHost host = hostBuilder.Build();
        IConfiguration configuration = host.Services.GetServices<IConfiguration>().SingleOrDefault();

        Assert.NotNull(configuration["foo:bar"]);
        Assert.Equal("value", configuration["foo:bar"]);

        Environment.SetEnvironmentVariable("SPRING_APPLICATION_JSON", string.Empty);
    }

    [Fact]
    public void GenericHostConfiguresIConfiguration_CmdLine()
    {
        IHostBuilder hostBuilder = Host.CreateDefaultBuilder(new[]
        {
            "Spring.Cloud.Stream.Bindings.Input.Destination=testDestination",
            "Spring.Cloud.Stream.Bindings.Input.Group=testGroup"
        }).AddSpringBootConfiguration();

        using IHost host = hostBuilder.Build();
        IConfiguration configuration = host.Services.GetServices<IConfiguration>().SingleOrDefault();

        Assert.NotNull(configuration["spring:cloud:stream:bindings:input:destination"]);
        Assert.Equal("testDestination", configuration["spring:cloud:stream:bindings:input:destination"]);

        Assert.NotNull(configuration["spring:cloud:stream:bindings:input:group"]);
        Assert.Equal("testGroup", configuration["spring:cloud:stream:bindings:input:group"]);
    }

    [Fact]
    public void ConfigureSpringBoot_WebApplicationBuilder_ThrowsIfNulls()
    {
        const WebApplicationBuilder webAppBuilder = null;

        Assert.Throws<ArgumentNullException>(() => webAppBuilder.AddSpringBootConfiguration());
    }

    [Fact]
    public void WebApplicationConfiguresIConfiguration_Spring_Application_Json()
    {
        Environment.SetEnvironmentVariable("SPRING_APPLICATION_JSON", "{\"foo.bar\":\"value\"}");
        WebApplicationBuilder hostBuilder = TestHelpers.GetTestWebApplicationBuilder();

        hostBuilder.AddSpringBootConfiguration();
        WebApplication host = hostBuilder.Build();

        var configuration = host.Services.GetService<IConfiguration>();

        Assert.NotNull(configuration["foo:bar"]);
        Assert.Equal("value", configuration["foo:bar"]);

        Environment.SetEnvironmentVariable("SPRING_APPLICATION_JSON", string.Empty);
    }

    [Fact]
    public void WebApplicationConfiguresIConfiguration_CmdLine()
    {
        WebApplicationBuilder hostBuilder = TestHelpers.GetTestWebApplicationBuilder(new[]
        {
            "Spring.Cloud.Stream.Bindings.Input.Destination=testDestination",
            "Spring.Cloud.Stream.Bindings.Input.Group=testGroup"
        });

        hostBuilder.AddSpringBootConfiguration();

        using WebApplication host = hostBuilder.Build();
        var configuration = host.Services.GetService<IConfiguration>();

        Assert.NotNull(configuration["spring:cloud:stream:bindings:input:destination"]);
        Assert.Equal("testDestination", configuration["spring:cloud:stream:bindings:input:destination"]);

        Assert.NotNull(configuration["spring:cloud:stream:bindings:input:group"]);
        Assert.Equal("testGroup", configuration["spring:cloud:stream:bindings:input:group"]);
    }
}
