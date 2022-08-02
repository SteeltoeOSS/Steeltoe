// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Xunit;

namespace Steeltoe.Extensions.Logging.DynamicLogger.Test;

public class DynamicLoggerHostBuilderExtensionsTest
{
    [Fact]
    public void AddDynamicLogging_IHostBuilder_AddsDynamicLogging()
    {
        IHostBuilder hostBuilder = new HostBuilder().AddDynamicLogging();

        IHost host = hostBuilder.Build();
        IEnumerable<ILoggerProvider> loggerProviders = host.Services.GetServices<ILoggerProvider>();

        Assert.Single(loggerProviders);
        Assert.IsType<DynamicConsoleLoggerProvider>(loggerProviders.First());
    }

    [Fact]
    public void AddDynamicLogging_IHostBuilder_RemovesConsoleLogging()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureLogging(ilb => ilb.AddConsole()).AddDynamicLogging();

        IHost host = hostBuilder.Build();
        IEnumerable<ILoggerProvider> loggerProviders = host.Services.GetServices<ILoggerProvider>();

        Assert.Single(loggerProviders);
        Assert.IsType<DynamicConsoleLoggerProvider>(loggerProviders.First());
    }

    [Fact]
    public void AddDynamicLogging_IHostBuilder_RemovesConsoleLoggingDefaultBuilder()
    {
        IHostBuilder hostBuilder = Host.CreateDefaultBuilder().ConfigureLogging(ilb => ilb.AddConsole()).AddDynamicLogging();

        IHost host = hostBuilder.Build();
        IEnumerable<ILoggerProvider> loggerProviders = host.Services.GetServices<ILoggerProvider>();

        Assert.DoesNotContain(loggerProviders, lp => lp is ConsoleLoggerProvider);
        Assert.Contains(loggerProviders, lp => lp is DynamicConsoleLoggerProvider);
    }

    [Fact]
    public void AddDynamicLogging_WebApplicationBuilder_AddsDynamicLogging()
    {
        WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.AddDynamicLogging();
        WebApplication host = hostBuilder.Build();
        IEnumerable<ILoggerProvider> loggerProviders = host.Services.GetServices<ILoggerProvider>();

        Assert.Single(loggerProviders.Where(provider => provider is DynamicConsoleLoggerProvider));
    }

    [Fact]
    public void AddDynamicLogging_WebApplicationBuilder_RemovesConsoleLogging()
    {
        WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.Logging.AddConsole();
        hostBuilder.AddDynamicLogging();

        WebApplication host = hostBuilder.Build();
        IEnumerable<ILoggerProvider> loggerProviders = host.Services.GetServices<ILoggerProvider>();

        Assert.DoesNotContain(loggerProviders, lp => lp is ConsoleLoggerProvider);
        Assert.Single(loggerProviders.Where(provider => provider is DynamicConsoleLoggerProvider));
    }
}
