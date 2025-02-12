// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Logging;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Logging.DynamicLogger.Test;

public sealed class HostBuilderTests
{
    [Fact]
    public async Task ReplacesExistingConsoleLoggerProvider()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Logging.AddJsonConsole();
        builder.Logging.AddDynamicConsole();
        await using WebApplication host = builder.Build();

        ILoggerProvider[] loggerProviders = [.. host.Services.GetServices<ILoggerProvider>()];
        loggerProviders.OfType<ConsoleLoggerProvider>().Should().BeEmpty();
        loggerProviders.OfType<DynamicConsoleLoggerProvider>().Should().ContainSingle();

        host.Services.GetService<IDynamicLoggerProvider>().Should().BeOfType<DynamicConsoleLoggerProvider>();
    }

    [Fact]
    public async Task DoesNotRegisterMultipleTimes()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Logging.AddDynamicConsole();
        builder.Logging.AddDynamicConsole();
        await using WebApplication host = builder.Build();

        ILoggerProvider[] loggerProviders = [.. host.Services.GetServices<ILoggerProvider>()];
        loggerProviders.OfType<DynamicConsoleLoggerProvider>().Should().ContainSingle();

        host.Services.GetService<IDynamicLoggerProvider>().Should().BeOfType<DynamicConsoleLoggerProvider>();
    }

    [Fact]
    public async Task DoesNotRegisterWhenOtherDynamicProviderIsAlreadyRegistered()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Services.AddSingleton<IDynamicLoggerProvider, OtherDynamicLoggerProvider>();
        builder.Logging.AddDynamicConsole();
        await using WebApplication host = builder.Build();

        host.Services.GetServices<ILoggerProvider>().Should().NotContain(provider => provider is DynamicConsoleLoggerProvider);
    }

    [Fact]
    public async Task CanChangeMinimumLogLevels()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Default"] = "Trace",
            ["Logging:LogLevel:Microsoft"] = "Warning",
            ["Logging:LogLevel:Steeltoe"] = "Error"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Logging.AddDynamicConsole();
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
        await using WebApplication host = builder.Build();

        var microsoftLogger = host.Services.GetRequiredService<ILogger<ApplicationBuilder>>();
        var steeltoeLogger = host.Services.GetRequiredService<ILogger<DynamicLoggerProvider>>();
        ILogger otherLogger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Fully.Qualified.Type");

        microsoftLogger.ProbeMinLevel().Should().Be(LogLevel.Warning);
        steeltoeLogger.ProbeMinLevel().Should().Be(LogLevel.Error);
        otherLogger.ProbeMinLevel().Should().Be(LogLevel.Trace);

        var dynamicProvider = host.Services.GetRequiredService<IDynamicLoggerProvider>();

        dynamicProvider.SetLogLevel("Microsoft.AspNetCore", LogLevel.Debug);
        dynamicProvider.SetLogLevel("Steeltoe.Logging", LogLevel.Critical);
        dynamicProvider.SetLogLevel("Fully", LogLevel.None);

        microsoftLogger.ProbeMinLevel().Should().Be(LogLevel.Debug);
        steeltoeLogger.ProbeMinLevel().Should().Be(LogLevel.Critical);
        otherLogger.ProbeMinLevel().Should().Be(LogLevel.None);

        dynamicProvider.SetLogLevel("Microsoft.AspNetCore", null);
        dynamicProvider.SetLogLevel("Steeltoe.Logging", null);
        dynamicProvider.SetLogLevel("Fully", null);

        microsoftLogger.ProbeMinLevel().Should().Be(LogLevel.Warning);
        steeltoeLogger.ProbeMinLevel().Should().Be(LogLevel.Error);
        otherLogger.ProbeMinLevel().Should().Be(LogLevel.Trace);
    }

    [Fact]
    public async Task TurnsOffConsoleColorsOnCloudFoundry()
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Logging.AddDynamicConsole();
        await using WebApplication host = builder.Build();

        var formatterOptions = host.Services.GetRequiredService<IOptions<SimpleConsoleFormatterOptions>>();
        formatterOptions.Value.ColorBehavior.Should().Be(LoggerColorBehavior.Disabled);
    }

    [Fact]
    public async Task DoesNotTurnOffConsoleColorsLocally()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Logging.AddDynamicConsole();
        await using WebApplication host = builder.Build();

        var formatterOptions = host.Services.GetRequiredService<IOptions<SimpleConsoleFormatterOptions>>();
        formatterOptions.Value.ColorBehavior.Should().NotBe(LoggerColorBehavior.Disabled);
    }

    [Fact]
    public async Task CanTurnOffConsoleColors()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Logging:Console:FormatterOptions:ColorBehavior"] = "Disabled"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Logging.AddDynamicConsole();
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
        await using WebApplication host = builder.Build();

        var formatterOptions = host.Services.GetRequiredService<IOptions<SimpleConsoleFormatterOptions>>();
        formatterOptions.Value.ColorBehavior.Should().Be(LoggerColorBehavior.Disabled);
    }

    [Fact]
    public async Task WorksWithBootstrapLogger()
    {
        var bootstrapLoggerFactory = BootstrapLoggerFactory.CreateConsole();

        ILogger<ApplicationBuilder> microsoftLogger = bootstrapLoggerFactory.CreateLogger<ApplicationBuilder>();
        ILogger<DynamicLoggerProvider> steeltoeLogger = bootstrapLoggerFactory.CreateLogger<DynamicLoggerProvider>();
        ILogger otherLogger = bootstrapLoggerFactory.CreateLogger("Fully.Qualified.Type");

        var appSettings = new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Default"] = "Trace",
            ["Logging:LogLevel:Microsoft"] = "Warning",
            ["Logging:LogLevel:Steeltoe"] = "Error"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Logging.AddDynamicConsole();
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
        builder.Services.UpgradeBootstrapLoggerFactory(bootstrapLoggerFactory);
        await using WebApplication host = builder.Build();

        await host.StartAsync();

        microsoftLogger.ProbeMinLevel().Should().Be(LogLevel.Warning);
        steeltoeLogger.ProbeMinLevel().Should().Be(LogLevel.Error);
        otherLogger.ProbeMinLevel().Should().Be(LogLevel.Trace);

        var dynamicProvider = host.Services.GetRequiredService<IDynamicLoggerProvider>();

        dynamicProvider.SetLogLevel("Microsoft.AspNetCore", LogLevel.Debug);
        dynamicProvider.SetLogLevel("Steeltoe.Logging", LogLevel.Critical);
        dynamicProvider.SetLogLevel("Fully", LogLevel.None);

        microsoftLogger.ProbeMinLevel().Should().Be(LogLevel.Debug);
        steeltoeLogger.ProbeMinLevel().Should().Be(LogLevel.Critical);
        otherLogger.ProbeMinLevel().Should().Be(LogLevel.None);

        dynamicProvider.SetLogLevel("Microsoft.AspNetCore", null);
        dynamicProvider.SetLogLevel("Steeltoe.Logging", null);
        dynamicProvider.SetLogLevel("Fully", null);

        microsoftLogger.ProbeMinLevel().Should().Be(LogLevel.Warning);
        steeltoeLogger.ProbeMinLevel().Should().Be(LogLevel.Error);
        otherLogger.ProbeMinLevel().Should().Be(LogLevel.Trace);
    }

    [Fact]
    public async Task DisposeTwiceSucceeds()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Logging.AddDynamicConsole();

        IDynamicLoggerProvider dynamicLoggerProvider;

        await using (WebApplication host = builder.Build())
        {
            dynamicLoggerProvider = host.Services.GetRequiredService<IDynamicLoggerProvider>();
        }

        Action action = dynamicLoggerProvider.Dispose;
        action.Should().NotThrow();
    }

    private sealed class OtherDynamicLoggerProvider : IDynamicLoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            throw new NotSupportedException();
        }

        public ICollection<DynamicLoggerState> GetLogLevels()
        {
            throw new NotSupportedException();
        }

        public void SetLogLevel(string categoryName, LogLevel? minLevel)
        {
        }

        public void RefreshConfiguration(LogLevelsConfiguration configuration)
        {
        }

        public void Dispose()
        {
        }
    }
}
