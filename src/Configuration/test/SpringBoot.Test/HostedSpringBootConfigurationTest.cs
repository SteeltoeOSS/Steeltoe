// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.Placeholder;

namespace Steeltoe.Configuration.SpringBoot.Test;

public sealed class HostedSpringBootConfigurationTest
{
    [Fact]
    public void WebHostConfiguresIConfiguration_Spring_Application_Json()
    {
        using var scope = new EnvironmentVariableScope("SPRING_APPLICATION_JSON", "{\"foo.bar\":\"value\"}");

        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.UseStartup<TestServerStartup>();
        hostBuilder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddSpringBootFromEnvironmentVariable());

        using var server = new TestServer(hostBuilder);
        var configuration = server.Services.GetRequiredService<IConfiguration>();

        Assert.NotNull(configuration["foo:bar"]);
        Assert.Equal("value", configuration["foo:bar"]);
    }

    [Fact]
    public void WebHostConfiguresIConfiguration_CmdLine()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();

        hostBuilder.UseConfiguration(new ConfigurationBuilder().AddCommandLine([
            "Spring.Cloud.Stream.Bindings.Input.Destination=testDestination",
            "Spring.Cloud.Stream.Bindings.Input.Group=testGroup"
        ]).Build());

        hostBuilder.UseStartup<TestServerStartup>();
        hostBuilder.ConfigureAppConfiguration((context, configurationBuilder) => configurationBuilder.AddSpringBootFromCommandLine(context.Configuration));

        using var server = new TestServer(hostBuilder);
        var configuration = server.Services.GetRequiredService<IConfiguration>();

        Assert.NotNull(configuration["spring:cloud:stream:bindings:input:destination"]);
        Assert.Equal("testDestination", configuration["spring:cloud:stream:bindings:input:destination"]);

        Assert.NotNull(configuration["spring:cloud:stream:bindings:input:group"]);
        Assert.Equal("testGroup", configuration["spring:cloud:stream:bindings:input:group"]);
    }

    [Fact]
    public void GenericHostConfiguresIConfiguration_Spring_Application_Json()
    {
        using var scope = new EnvironmentVariableScope("SPRING_APPLICATION_JSON", "{\"foo.bar\":\"value\"}");

        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddSpringBootFromEnvironmentVariable());

        using IHost host = hostBuilder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        Assert.NotNull(configuration["foo:bar"]);
        Assert.Equal("value", configuration["foo:bar"]);
    }

    [Fact]
    public void GenericHostConfiguresIConfiguration_Spring_Application_Json_Works_With_Placeholder()
    {
        using var scope = new EnvironmentVariableScope("SPRING_APPLICATION_JSON", "{\"foo.bar\":\"value\"}");

        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();

        hostBuilder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.AddSpringBootFromEnvironmentVariable();
            configurationBuilder.AddPlaceholderResolver();
        });

        using IHost host = hostBuilder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        Assert.NotNull(configuration["foo:bar"]);
        Assert.Equal("value", configuration["foo:bar"]);
    }

    [Fact]
    public void GenericHostConfiguresIConfiguration_CmdLine()
    {
        IHostBuilder hostBuilder = Host.CreateDefaultBuilder([
            "Spring.Cloud.Stream.Bindings.Input.Destination=testDestination",
            "Spring.Cloud.Stream.Bindings.Input.Group=testGroup"
        ]);

        hostBuilder.ConfigureAppConfiguration((context, configurationBuilder) => configurationBuilder.AddSpringBootFromCommandLine(context.Configuration));

        using IHost host = hostBuilder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        Assert.NotNull(configuration["spring:cloud:stream:bindings:input:destination"]);
        Assert.Equal("testDestination", configuration["spring:cloud:stream:bindings:input:destination"]);

        Assert.NotNull(configuration["spring:cloud:stream:bindings:input:group"]);
        Assert.Equal("testGroup", configuration["spring:cloud:stream:bindings:input:group"]);
    }

    [Fact]
    public async Task WebApplicationConfiguresIConfiguration_Spring_Application_Json()
    {
        using var scope = new EnvironmentVariableScope("SPRING_APPLICATION_JSON", "{\"foo.bar\":\"value\"}");

        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create();
        hostBuilder.Configuration.AddSpringBootFromEnvironmentVariable();

        await using WebApplication host = hostBuilder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        Assert.NotNull(configuration["foo:bar"]);
        Assert.Equal("value", configuration["foo:bar"]);
    }

    [Fact]
    public async Task WebApplicationConfiguresIConfiguration_CmdLine()
    {
        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create([
            "Spring.Cloud.Stream.Bindings.Input.Destination=testDestination",
            "Spring.Cloud.Stream.Bindings.Input.Group=testGroup"
        ]);

        hostBuilder.Configuration.AddSpringBootFromCommandLine(hostBuilder.Configuration);

        await using WebApplication host = hostBuilder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        Assert.NotNull(configuration["spring:cloud:stream:bindings:input:destination"]);
        Assert.Equal("testDestination", configuration["spring:cloud:stream:bindings:input:destination"]);

        Assert.NotNull(configuration["spring:cloud:stream:bindings:input:group"]);
        Assert.Equal("testGroup", configuration["spring:cloud:stream:bindings:input:group"]);
    }
}
