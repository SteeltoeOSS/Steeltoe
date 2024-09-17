// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Logging.DynamicLogger.Test;

public sealed class DynamicLoggerHostBuilderExtensionsTest
{
    [Fact]
    public void AddDynamicLogging_IHostBuilder_AddsDynamicLogging()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.ConfigureLogging(builder => builder.AddDynamicConsole());

        using IHost host = hostBuilder.Build();
        ILoggerProvider[] loggerProviders = host.Services.GetServices<ILoggerProvider>().ToArray();

        loggerProviders.Should().HaveCount(1);
        loggerProviders[0].Should().BeOfType<DynamicConsoleLoggerProvider>();
    }

    [Fact]
    public void AddDynamicLogging_IHostBuilder_RemovesConsoleLogging()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();

        hostBuilder.ConfigureLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDynamicConsole();
        });

        using IHost host = hostBuilder.Build();
        ILoggerProvider[] loggerProviders = host.Services.GetServices<ILoggerProvider>().ToArray();

        loggerProviders.Should().HaveCount(1);
        loggerProviders[0].Should().BeOfType<DynamicConsoleLoggerProvider>();
    }

    [Fact]
    public void AddDynamicLogging_IHostBuilder_RemovesConsoleLoggingDefaultBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();

        hostBuilder.ConfigureLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDynamicConsole();
        });

        using IHost host = hostBuilder.Build();
        ILoggerProvider[] loggerProviders = host.Services.GetServices<ILoggerProvider>().ToArray();

        loggerProviders.Should().NotContain(provider => provider is ConsoleLoggerProvider);
        loggerProviders.Should().ContainSingle(provider => provider is DynamicConsoleLoggerProvider);
    }

    [Fact]
    public async Task AddDynamicLogging_WebApplicationBuilder_AddsDynamicLogging()
    {
        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create();
        hostBuilder.Logging.AddDynamicConsole();
        await using WebApplication host = hostBuilder.Build();

        ILoggerProvider[] loggerProviders = host.Services.GetServices<ILoggerProvider>().ToArray();

        loggerProviders.Should().ContainSingle(provider => provider is DynamicConsoleLoggerProvider);
    }

    [Fact]
    public async Task AddDynamicLogging_WebApplicationBuilder_RemovesConsoleLogging()
    {
        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create();
        hostBuilder.Logging.AddConsole();
        hostBuilder.Logging.AddDynamicConsole();
        await using WebApplication host = hostBuilder.Build();

        ILoggerProvider[] loggerProviders = host.Services.GetServices<ILoggerProvider>().ToArray();

        loggerProviders.Should().NotContain(provider => provider is ConsoleLoggerProvider);
        loggerProviders.Should().ContainSingle(provider => provider is DynamicConsoleLoggerProvider);
    }
}
