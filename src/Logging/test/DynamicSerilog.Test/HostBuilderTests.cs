// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Steeltoe.Common.TestResources;
using Steeltoe.Logging.DynamicConsole;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Steeltoe.Logging.DynamicSerilog.Test;

public sealed class HostBuilderTests : IDisposable
{
    private readonly ConsoleOutput _consoleOutput = ConsoleOutput.Capture();

    public HostBuilderTests()
    {
        DynamicSerilogLoggerProvider.ClearLogger();
    }

    [Fact]
    public async Task ReplacesExistingConsoleLoggerProvider()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Logging.AddJsonConsole();
        builder.Logging.AddDynamicSerilog();
        await using WebApplication host = builder.Build();

        ILoggerProvider[] loggerProviders = [.. host.Services.GetServices<ILoggerProvider>()];
        loggerProviders.OfType<ConsoleLoggerProvider>().Should().BeEmpty();
        loggerProviders.OfType<DynamicSerilogLoggerProvider>().Should().ContainSingle();

        host.Services.GetService<IDynamicLoggerProvider>().Should().BeOfType<DynamicSerilogLoggerProvider>();
    }

    [Fact]
    public async Task DoesNotRegisterMultipleTimes()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Logging.AddDynamicSerilog();
        builder.Logging.AddDynamicSerilog();
        await using WebApplication host = builder.Build();

        ILoggerProvider[] loggerProviders = [.. host.Services.GetServices<ILoggerProvider>()];
        loggerProviders.OfType<DynamicSerilogLoggerProvider>().Should().ContainSingle();

        host.Services.GetService<IDynamicLoggerProvider>().Should().BeOfType<DynamicSerilogLoggerProvider>();
    }

    [Fact]
    public void FailsWhenDynamicConsoleLoggerAlreadyRegistered()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Logging.AddDynamicConsole();

        Action action = () => builder.Logging.AddDynamicSerilog();

        action.Should().ThrowExactly<InvalidOperationException>().WithMessage(
            "A different IDynamicLoggerProvider has already been registered. Call 'AddDynamicSerilog' earlier during startup (before adding actuators).");
    }

    [Fact]
    public async Task CanPreserveDefaultConsoleLoggerProvider()
    {
        Dictionary<string, string?> appSettings = new()
        {
            ["Serilog:WriteTo:0:Name"] = "Console",
            ["Serilog:WriteTo:0:Args:OutputTemplate"] = "SERILOG [{Level:u3}] {Message:lj}{NewLine}"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Logging.AddSimpleConsole();
        builder.Logging.AddDynamicSerilog(true);
        await using WebApplication host = builder.Build();

        var logger = host.Services.GetRequiredService<ILogger<HostBuilderTests>>();
        logger.LogTrace("TestTrace");
        logger.LogInformation("TestInfo");
        logger.LogError("TestError");

        await _consoleOutput.WaitForFlushAsync(TestContext.Current.CancellationToken);
        string logOutput = _consoleOutput.ToString();

        logOutput.Should().Contain("SERILOG [INF] TestInfo");
        logOutput.Should().Contain("SERILOG [ERR] TestError");

        logOutput.Should().Contain($"""
            info: {typeof(HostBuilderTests).FullName}[0]
                  TestInfo
            """);

        logOutput.Should().Contain($"""
            fail: {typeof(HostBuilderTests).FullName}[0]
                  TestError
            """);
    }

    [Fact]
    public async Task CanConfigureSerilogWithoutLevelsConfiguration()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Serilog:WriteTo:0:Name"] = "Console",
            ["Serilog:WriteTo:0:Args:OutputTemplate"] = "[{Level:u3}] {Message:lj}{NewLine}"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Logging.AddDynamicSerilog();
        await using WebApplication host = builder.Build();

        var optionsMonitor = host.Services.GetRequiredService<IOptionsMonitor<SerilogOptions>>();
        MinimumLevel? minimumLevel = optionsMonitor.CurrentValue.MinimumLevel;
        minimumLevel.Should().NotBeNull();
        minimumLevel.Default.Should().Be(LogEventLevel.Information);
        minimumLevel.Override.Should().BeEmpty();

        var logger = host.Services.GetRequiredService<ILogger<HostBuilderTests>>();
        logger.LogTrace("TestTrace");
        logger.LogInformation("TestInfo");
        logger.LogError("TestError");

        string logOutput = _consoleOutput.ToString();

        logOutput.Should().Be("""
            [INF] TestInfo
            [ERR] TestError

            """);
    }

    [Fact]
    public async Task CanConfigureSerilogFromConfigurationWithDefaultLevel()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Serilog:MinimumLevel:Default"] = "Error",
            ["Serilog:WriteTo:0:Name"] = "Console",
            ["Serilog:WriteTo:0:Args:OutputTemplate"] = "[{Level:u3}] {Message:lj}{NewLine}"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Logging.AddDynamicSerilog();
        await using WebApplication host = builder.Build();

        var optionsMonitor = host.Services.GetRequiredService<IOptionsMonitor<SerilogOptions>>();
        MinimumLevel? minimumLevel = optionsMonitor.CurrentValue.MinimumLevel;
        minimumLevel.Should().NotBeNull();
        minimumLevel.Default.Should().Be(LogEventLevel.Error);
        minimumLevel.Override.Should().BeEmpty();

        var logger = host.Services.GetRequiredService<ILogger<HostBuilderTests>>();
        logger.LogTrace("TestTrace");
        logger.LogInformation("TestInfo");
        logger.LogError("TestError");

        string logOutput = _consoleOutput.ToString();

        logOutput.Should().Be("""
            [ERR] TestError

            """);
    }

    [Fact]
    public async Task CanConfigureSerilogFromConfigurationWithShortKeyForDefaultLevel()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Serilog:MinimumLevel"] = "Error",
            ["Serilog:WriteTo:0:Name"] = "Console",
            ["Serilog:WriteTo:0:Args:OutputTemplate"] = "[{Level:u3}] {Message:lj}{NewLine}"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Logging.AddDynamicSerilog();
        await using WebApplication host = builder.Build();

        var optionsMonitor = host.Services.GetRequiredService<IOptionsMonitor<SerilogOptions>>();
        MinimumLevel? minimumLevel = optionsMonitor.CurrentValue.MinimumLevel;
        minimumLevel.Should().NotBeNull();
        minimumLevel.Default.Should().Be(LogEventLevel.Error);
        minimumLevel.Override.Should().BeEmpty();

        var logger = host.Services.GetRequiredService<ILogger<HostBuilderTests>>();
        logger.LogTrace("TestTrace");
        logger.LogInformation("TestInfo");
        logger.LogError("TestError");

        string logOutput = _consoleOutput.ToString();

        logOutput.Should().Be("""
            [ERR] TestError

            """);
    }

    [Fact]
    public async Task CanConfigureSerilogFromConfigurationWithOnlyOverrides()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Serilog:MinimumLevel:Override:Steeltoe"] = "Error",
            ["Serilog:WriteTo:0:Name"] = "Console",
            ["Serilog:WriteTo:0:Args:OutputTemplate"] = "[{Level:u3}] {Message:lj}{NewLine}"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Logging.AddDynamicSerilog();
        await using WebApplication host = builder.Build();

        var optionsMonitor = host.Services.GetRequiredService<IOptionsMonitor<SerilogOptions>>();
        MinimumLevel? minimumLevel = optionsMonitor.CurrentValue.MinimumLevel;
        minimumLevel.Should().NotBeNull();
        minimumLevel.Default.Should().Be(LogEventLevel.Information);
        minimumLevel.Override.Should().ContainSingle();
        minimumLevel.Override.Should().Contain("Steeltoe", LogEventLevel.Error);

        var logger = host.Services.GetRequiredService<ILogger<HostBuilderTests>>();
        logger.LogTrace("TestTrace");
        logger.LogInformation("TestInfo");
        logger.LogError("TestError");

        string logOutput = _consoleOutput.ToString();

        logOutput.Should().Be("""
            [ERR] TestError

            """);
    }

    [Fact]
    public async Task CanConfigureSerilogFromCodeWithDefaultLevel()
    {
        // @formatter:wrap_chained_method_calls chop_always
        // @formatter:wrap_before_first_method_call true

        LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Error()
            .Enrich.WithExceptionDetails()
            .WriteTo.Console(outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}");

        // @formatter:wrap_before_first_method_call restore
        // @formatter:wrap_chained_method_calls restore

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Logging.AddDynamicSerilog(loggerConfiguration);
        await using WebApplication host = builder.Build();

        var optionsMonitor = host.Services.GetRequiredService<IOptionsMonitor<SerilogOptions>>();
        MinimumLevel? minimumLevel = optionsMonitor.CurrentValue.MinimumLevel;
        minimumLevel.Should().NotBeNull();
        minimumLevel.Default.Should().Be(LogEventLevel.Error);
        minimumLevel.Override.Should().BeEmpty();

        var logger = host.Services.GetRequiredService<ILogger<HostBuilderTests>>();
        logger.LogTrace("TestTrace");
        logger.LogInformation("TestInfo");
        logger.LogError("TestError");

        string logOutput = _consoleOutput.ToString();

        logOutput.Should().Be("""
            [ERR] TestError

            """);
    }

    [Fact]
    public async Task CanConfigureSerilogFromCodeWithOnlyOverrides()
    {
        // @formatter:wrap_chained_method_calls chop_always
        // @formatter:wrap_before_first_method_call true

        LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Override("Steeltoe", LogEventLevel.Error)
            .Enrich.WithExceptionDetails()
            .WriteTo.Console(outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}");

        // @formatter:wrap_before_first_method_call restore
        // @formatter:wrap_chained_method_calls restore

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Logging.AddDynamicSerilog(loggerConfiguration);
        await using WebApplication host = builder.Build();

        var optionsMonitor = host.Services.GetRequiredService<IOptionsMonitor<SerilogOptions>>();
        MinimumLevel? minimumLevel = optionsMonitor.CurrentValue.MinimumLevel;
        minimumLevel.Should().NotBeNull();
        minimumLevel.Default.Should().Be(LogEventLevel.Information);
        minimumLevel.Override.Should().ContainSingle();
        minimumLevel.Override.Should().Contain("Steeltoe", LogEventLevel.Error);

        var logger = host.Services.GetRequiredService<ILogger<HostBuilderTests>>();
        logger.LogTrace("TestTrace");
        logger.LogInformation("TestInfo");
        logger.LogError("TestError");

        string logOutput = _consoleOutput.ToString();

        logOutput.Should().Be("""
            [ERR] TestError

            """);
    }

    [Fact]
    public async Task CanChangeMinimumLogLevels()
    {
        // @formatter:wrap_chained_method_calls chop_always
        // @formatter:wrap_before_first_method_call true

        LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Steeltoe", LogEventLevel.Error);

        // @formatter:wrap_before_first_method_call restore
        // @formatter:wrap_chained_method_calls restore

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Logging.AddDynamicSerilog(loggerConfiguration);
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
    public async Task DynamicLevelChangeDoesNotAffectUsageOfNativeSerilogApi()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Serilog:WriteTo:0:Name"] = "Console",
            ["Serilog:WriteTo:0:Args:OutputTemplate"] = "[{Level:u3}] {Message:lj}{NewLine}"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Logging.AddDynamicSerilog();
        await using WebApplication host = builder.Build();

        Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
        Log.Logger.Information("TestInfoBefore");

        var loggerProvider = host.Services.GetRequiredService<IDynamicLoggerProvider>();
        loggerProvider.SetLogLevel(string.Empty, LogLevel.Critical);

        Log.Logger.Information("TestInfoAfter");

        string logOutput = _consoleOutput.ToString();
        logOutput.Should().Contain("TestInfoBefore");
        logOutput.Should().Contain("TestInfoAfter");
    }

    [Fact]
    public async Task DisposeTwiceSucceeds()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Logging.AddDynamicSerilog();

        IDynamicLoggerProvider dynamicLoggerProvider;

        await using (WebApplication host = builder.Build())
        {
            dynamicLoggerProvider = host.Services.GetRequiredService<IDynamicLoggerProvider>();
        }

        Action action = dynamicLoggerProvider.Dispose;
        action.Should().NotThrow();
    }

    public void Dispose()
    {
        _consoleOutput.Dispose();
    }
}
