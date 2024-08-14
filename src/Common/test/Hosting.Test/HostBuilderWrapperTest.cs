// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Common.Hosting.Test;

public sealed class HostBuilderWrapperTest
{
    [Fact]
    public void WebApplicationBuilder_Wraps()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["foo"] = "bar"
        };

        var capturingLoggerProvider = new CapturingLoggerProvider(category => category.StartsWith("Test", StringComparison.Ordinal));

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.ConfigureServices(services => services.AddSingleton<InjectableType>());
        wrapper.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
        wrapper.ConfigureLogging(loggingBuilder => loggingBuilder.AddProvider(capturingLoggerProvider));
        wrapper.ConfigureWebHost(hostBuilder => hostBuilder.UseUrls("http://*:8888"));
        wrapper.ConfigureServices((contextWrapper, _) => contextWrapper.HostEnvironment.ApplicationName = "TestApp");

        WebApplication app = builder.Build();

        app.Services.GetService<InjectableType>().Should().NotBeNull();
        app.Configuration.GetValue<string>("foo").Should().Be("bar");

        var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
        ILogger logger = loggerFactory.CreateLogger("Test");

        logger.LogInformation("LogLine");
        capturingLoggerProvider.GetAll().Should().Contain("INFO Test: LogLine");

        app.Configuration.GetValue<string>("urls").Should().Be("http://*:8888");

        app.Environment.ApplicationName.Should().Be("TestApp");
    }

    [Fact]
    public void WebHostBuilder_Wraps()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["foo"] = "bar"
        };

        var capturingLoggerProvider = new CapturingLoggerProvider(category => category.StartsWith("Test", StringComparison.Ordinal));

        IWebHostBuilder builder = new WebHostBuilder().Configure(HostingHelpers.EmptyAction);
        builder.UseDefaultServiceProvider(options => options.ValidateScopes = true);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.ConfigureServices(services => services.AddSingleton<InjectableType>());
        wrapper.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
        wrapper.ConfigureLogging(loggingBuilder => loggingBuilder.AddProvider(capturingLoggerProvider));
        wrapper.ConfigureWebHost(hostBuilder => hostBuilder.UseUrls("http://*:8888"));
        wrapper.ConfigureServices((contextWrapper, _) => contextWrapper.HostEnvironment.ApplicationName = "TestApp");

        IWebHost app = builder.Build();

        app.Services.GetService<InjectableType>().Should().NotBeNull();
        var configuration = app.Services.GetRequiredService<IConfiguration>();
        configuration.GetValue<string>("foo").Should().Be("bar");

        var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
        ILogger logger = loggerFactory.CreateLogger("Test");

        logger.LogInformation("LogLine");
        capturingLoggerProvider.GetAll().Should().Contain("INFO Test: LogLine");

        configuration.GetValue<string>("urls").Should().Be("http://*:8888");

        var webHostEnvironment = app.Services.GetRequiredService<IWebHostEnvironment>();
        webHostEnvironment.ApplicationName.Should().Be("TestApp");
    }

    [Fact]
    public void GenericHostBuilder_Wraps()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["foo"] = "bar"
        };

        var capturingLoggerProvider = new CapturingLoggerProvider(category => category.StartsWith("Test", StringComparison.Ordinal));

        IHostBuilder builder = new HostBuilder();
        builder.UseDefaultServiceProvider(options => options.ValidateScopes = true);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.ConfigureServices(services => services.AddSingleton<InjectableType>());
        wrapper.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
        wrapper.ConfigureLogging(loggingBuilder => loggingBuilder.AddProvider(capturingLoggerProvider));
        wrapper.ConfigureWebHost(hostBuilder => hostBuilder.UseUrls("http://*:8888"));
        wrapper.ConfigureServices((contextWrapper, _) => contextWrapper.HostEnvironment.ApplicationName = "TestApp");

        IHost app = builder.Build();

        app.Services.GetService<InjectableType>().Should().NotBeNull();
        var configuration = app.Services.GetRequiredService<IConfiguration>();
        configuration.GetValue<string>("foo").Should().Be("bar");

        var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
        ILogger logger = loggerFactory.CreateLogger("Test");

        logger.LogInformation("LogLine");
        capturingLoggerProvider.GetAll().Should().Contain("INFO Test: LogLine");

        configuration.GetValue<string>("urls").Should().Be("http://*:8888");

        var hostEnvironment = app.Services.GetRequiredService<IHostEnvironment>();
        hostEnvironment.ApplicationName.Should().Be("TestApp");
    }

    private sealed class InjectableType;
}
