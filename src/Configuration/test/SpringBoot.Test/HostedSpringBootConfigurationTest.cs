// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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

        WebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddSpringBootFromEnvironmentVariable());

        using IWebHost app = hostBuilder.Build();
        var configuration = app.Services.GetRequiredService<IConfiguration>();

        configuration["foo:bar"].Should().Be("value");
    }

    [Fact]
    public void WebHostConfiguresIConfiguration_CmdLine()
    {
        string[] args =
        [
            "Spring.Cloud.Stream.Bindings.Input.Destination=testDestination",
            "Spring.Cloud.Stream.Bindings.Input.Group=testGroup"
        ];

        WebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddSpringBootFromCommandLine(args));

        using IWebHost app = hostBuilder.Build();
        var configuration = app.Services.GetRequiredService<IConfiguration>();

        configuration["spring:cloud:stream:bindings:input:destination"].Should().Be("testDestination");
        configuration["spring:cloud:stream:bindings:input:group"].Should().Be("testGroup");
    }

    [Fact]
    public void GenericHostConfiguresIConfiguration_Spring_Application_Json()
    {
        using var scope = new EnvironmentVariableScope("SPRING_APPLICATION_JSON", "{\"foo.bar\":\"value\"}");

        HostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddSpringBootFromEnvironmentVariable());

        using IHost host = hostBuilder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        configuration["foo:bar"].Should().Be("value");
    }

    [Fact]
    public void GenericHostConfiguresIConfiguration_Spring_Application_Json_Works_With_Placeholder()
    {
        using var scope = new EnvironmentVariableScope("SPRING_APPLICATION_JSON", "{\"foo.bar\":\"value\"}");

        HostBuilder hostBuilder = TestHostBuilderFactory.Create();

        hostBuilder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.AddSpringBootFromEnvironmentVariable();
            configurationBuilder.AddPlaceholderResolver();
        });

        using IHost host = hostBuilder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        configuration["foo:bar"].Should().Be("value");
    }

    [Fact]
    public void GenericHostConfiguresIConfiguration_CmdLine()
    {
        string[] args =
        [
            "Spring.Cloud.Stream.Bindings.Input.Destination=testDestination",
            "Spring.Cloud.Stream.Bindings.Input.Group=testGroup"
        ];

        IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);
        hostBuilder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddSpringBootFromCommandLine(args));

        using IHost host = hostBuilder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        configuration["spring:cloud:stream:bindings:input:destination"].Should().Be("testDestination");
        configuration["spring:cloud:stream:bindings:input:group"].Should().Be("testGroup");
    }

    [Fact]
    public async Task WebApplicationConfiguresIConfiguration_Spring_Application_Json()
    {
        using var scope = new EnvironmentVariableScope("SPRING_APPLICATION_JSON", "{\"foo.bar\":\"value\"}");

        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create();
        hostBuilder.Configuration.AddSpringBootFromEnvironmentVariable();

        await using WebApplication host = hostBuilder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        configuration["foo:bar"].Should().Be("value");
    }

    [Fact]
    public async Task WebApplicationConfiguresIConfiguration_CmdLine()
    {
        string[] args =
        [
            "Spring.Cloud.Stream.Bindings.Input.Destination=testDestination",
            "Spring.Cloud.Stream.Bindings.Input.Group=testGroup"
        ];

        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create(args);
        hostBuilder.Configuration.AddSpringBootFromCommandLine(args);

        await using WebApplication host = hostBuilder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        configuration["spring:cloud:stream:bindings:input:destination"].Should().Be("testDestination");
        configuration["spring:cloud:stream:bindings:input:group"].Should().Be("testGroup");
    }
}
