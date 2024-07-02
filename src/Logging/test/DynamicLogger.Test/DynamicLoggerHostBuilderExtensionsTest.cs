// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Steeltoe.Logging.DynamicLogger.Test;

public sealed class DynamicLoggerHostBuilderExtensionsTest
{
    [Fact]
    public void AddDynamicLogging_IHostBuilder_AddsDynamicLogging()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureLogging(builder => builder.AddDynamicConsole());

        IHost host = hostBuilder.Build();
        ILoggerProvider[] loggerProviders = host.Services.GetServices<ILoggerProvider>().ToArray();

        loggerProviders.Should().HaveCount(1);
        loggerProviders[0].Should().BeOfType<DynamicConsoleLoggerProvider>();
    }

    [Fact]
    public void AddDynamicLogging_IHostBuilder_RemovesConsoleLogging()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDynamicConsole();
        });

        IHost host = hostBuilder.Build();
        ILoggerProvider[] loggerProviders = host.Services.GetServices<ILoggerProvider>().ToArray();

        loggerProviders.Should().HaveCount(1);
        loggerProviders[0].Should().BeOfType<DynamicConsoleLoggerProvider>();
    }

    [Fact]
    public void AddDynamicLogging_IHostBuilder_RemovesConsoleLoggingDefaultBuilder()
    {
        IHostBuilder hostBuilder = Host.CreateDefaultBuilder().UseDefaultServiceProvider(options => options.ValidateScopes = true).ConfigureLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDynamicConsole();
        });

        IHost host = hostBuilder.Build();
        ILoggerProvider[] loggerProviders = host.Services.GetServices<ILoggerProvider>().ToArray();

        loggerProviders.Should().NotContain(provider => provider is ConsoleLoggerProvider);
        loggerProviders.Should().ContainSingle(provider => provider is DynamicConsoleLoggerProvider);
    }

    [Fact]
    public void AddDynamicLogging_WebApplicationBuilder_AddsDynamicLogging()
    {
        WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.Host.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        hostBuilder.Logging.AddDynamicConsole();
        WebApplication host = hostBuilder.Build();

        ILoggerProvider[] loggerProviders = host.Services.GetServices<ILoggerProvider>().ToArray();

        loggerProviders.Should().ContainSingle(provider => provider is DynamicConsoleLoggerProvider);
    }

    [Fact]
    public void AddDynamicLogging_WebApplicationBuilder_RemovesConsoleLogging()
    {
        WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.Host.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        hostBuilder.Logging.AddConsole();
        hostBuilder.Logging.AddDynamicConsole();
        WebApplication host = hostBuilder.Build();

        ILoggerProvider[] loggerProviders = host.Services.GetServices<ILoggerProvider>().ToArray();

        loggerProviders.Should().NotContain(provider => provider is ConsoleLoggerProvider);
        loggerProviders.Should().ContainSingle(provider => provider is DynamicConsoleLoggerProvider);
    }
}
