// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using A.B.C.D;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Steeltoe.Logging.DynamicSerilog.Test;

public sealed class SerilogDynamicLoggingBuilderTest
{
    private static readonly Dictionary<string, string?> Appsettings = new()
    {
        ["Serilog:MinimumLevel:Default"] = "Verbose",
        ["Serilog:MinimumLevel:Override:Microsoft"] = "Warning",
        ["Serilog:MinimumLevel:Override:Steeltoe.Extensions"] = "Verbose",
        ["Serilog:MinimumLevel:Override:Steeltoe"] = "Information",
        ["Serilog:MinimumLevel:Override:A"] = "Information",
        ["Serilog:MinimumLevel:Override:A.B.C.D"] = "Fatal",
        ["Serilog:WriteTo:Name"] = "Console"
    };

    public SerilogDynamicLoggingBuilderTest()
    {
        DynamicSerilogLoggerProvider.ClearLogger();
    }

    [Fact]
    public void OnlyApplicableFilters_AreApplied()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Serilog:MinimumLevel:Default"] = "Information"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        ServiceProvider serviceProvider = new ServiceCollection().AddSingleton(configuration).AddLogging(builder =>
        {
            builder.AddDynamicSerilog();
        }).BuildServiceProvider(true);

        var logger = serviceProvider.GetRequiredService<ILogger<TestClass>>();

        logger.IsEnabled(LogLevel.Critical).Should().BeTrue();
        logger.IsEnabled(LogLevel.Error).Should().BeTrue();
        logger.IsEnabled(LogLevel.Warning).Should().BeTrue();
        logger.IsEnabled(LogLevel.Information).Should().BeTrue();
        logger.IsEnabled(LogLevel.Debug).Should().BeFalse();
        logger.IsEnabled(LogLevel.Trace).Should().BeFalse();
        logger.IsEnabled(LogLevel.None).Should().BeFalse();
    }

    [Fact]
    public void DynamicLevelSetting_WorksWith_ConsoleFilters()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();

        ServiceProvider serviceProvider = new ServiceCollection().AddSingleton(configuration).AddLogging(builder =>
        {
            builder.AddDynamicSerilog();
        }).BuildServiceProvider(true);

        var logger = serviceProvider.GetRequiredService<ILogger<TestClass>>();

        logger.IsEnabled(LogLevel.Critical).Should().BeTrue();
        logger.IsEnabled(LogLevel.Error).Should().BeFalse();
        logger.IsEnabled(LogLevel.Warning).Should().BeFalse();
        logger.IsEnabled(LogLevel.Information).Should().BeFalse();
        logger.IsEnabled(LogLevel.Debug).Should().BeFalse();
        logger.IsEnabled(LogLevel.Trace).Should().BeFalse();
        logger.IsEnabled(LogLevel.None).Should().BeFalse();

        // change the log level and confirm it worked
        var provider = (DynamicSerilogLoggerProvider)serviceProvider.GetRequiredService<ILoggerProvider>();
        provider.SetLogLevel("A.B.C.D", LogLevel.Trace);

        LogLevel[] levels = provider.GetLoggerConfigurations().Where(entry => entry.CategoryName.StartsWith("A.B.C.D", StringComparison.Ordinal))
            .Select(entry => entry.EffectiveMinLevel).ToArray();

        levels.Should().NotBeEmpty();
        levels.Should().AllSatisfy(level => level.Should().Be(LogLevel.Trace));
    }

    [Fact]
    public void AddDynamicSerilogPreservesDefaultLoggerWhenTrue()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();
        var serviceProvider = new ServiceCollection();

        serviceProvider.AddSingleton(configuration).AddSingleton<ConsoleLoggerProvider>().AddLogging(builder =>
        {
            builder.AddDynamicSerilog(true);
        }).BuildServiceProvider(true);

        serviceProvider.Should().Contain(descriptor => descriptor.ImplementationType == typeof(ConsoleLoggerProvider));
    }

    [Fact]
    public void AddDynamicConsole_AddsAllLoggerProviders()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();

        ServiceProvider serviceProvider = new ServiceCollection().AddSingleton(configuration).AddLogging(builder =>
        {
            builder.AddDynamicSerilog();
        }).BuildServiceProvider(true);

        var dynamicLoggerProvider = serviceProvider.GetService<IDynamicLoggerProvider>();
        ILoggerProvider[] loggerProviders = serviceProvider.GetServices<ILoggerProvider>().ToArray();

        dynamicLoggerProvider.Should().NotBeNull();

        loggerProviders.Should().HaveCount(1);
        loggerProviders[0].Should().BeOfType<DynamicSerilogLoggerProvider>();
    }

    [Fact]
    public void AddDynamicConsole_AddsLoggerProvider_DisposeTwiceSucceeds()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();

        ServiceProvider serviceProvider = new ServiceCollection().AddSingleton(configuration).AddLogging(builder =>
        {
            builder.AddDynamicSerilog();
        }).BuildServiceProvider(true);

        var dynamicLoggerProvider = serviceProvider.GetRequiredService<IDynamicLoggerProvider>();

        serviceProvider.Dispose();

        Action action = () => dynamicLoggerProvider.Dispose();
        action.Should().NotThrow();
    }

    [Fact]
    public void AddDynamicConsole_WithConfigurationParam_AddsServices()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();

        ServiceProvider serviceProvider = new ServiceCollection().AddSingleton(configuration).AddLogging(builder => builder.AddDynamicSerilog())
            .BuildServiceProvider(true);

        var dynamicLoggerProvider = serviceProvider.GetService<IDynamicLoggerProvider>();
        ILoggerProvider[] logProviders = serviceProvider.GetServices<ILoggerProvider>().ToArray();

        dynamicLoggerProvider.Should().NotBeNull();

        logProviders.Should().HaveCount(1);
        logProviders[0].Should().BeOfType<DynamicSerilogLoggerProvider>();
    }

    [Fact]
    public void AddDynamicConsole_WithDynamicMessageProcessor_CallsProcessMessage()
    {
        using var console = new ConsoleOutputBorrower();

        IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("serilogSettings.json").Build();

        ServiceProvider serviceProvider = new ServiceCollection().AddSingleton(configuration)
            .AddSingleton<IDynamicMessageProcessor, TestDynamicMessageProcessor>().AddLogging(builder => builder.AddDynamicSerilog())
            .BuildServiceProvider(true);

        var logger = serviceProvider.GetRequiredService<ILogger<SerilogDynamicLoggingBuilderTest>>();
        logger.LogInformation("This is a test");

        string log = console.ToString();

        log.Should().Contain("{Scope=[\"<<<[]>>>\"], Application=\"Sample\"}");
        log.Should().Contain("This is a test");
    }

    [Fact]
    public void AddDynamicConsole_IncludesScopes()
    {
        using var console = new ConsoleOutputBorrower();

        IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("serilogSettings.json").Build();

        ServiceProvider serviceProvider = new ServiceCollection().AddSingleton(configuration).AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicSerilog();
        }).BuildServiceProvider(true);

        var logger = serviceProvider.GetRequiredService<ILogger<SerilogDynamicLoggingBuilderTest>>();

        using (logger.BeginScope("Outer Scope"))
        {
            using (logger.BeginScope("InnerScopeKey={ScopeValue}", "InnerScopeValue"))
            {
                logger.LogError("Something bad.");
            }
        }

        string log = console.ToString();

        log.Should().Contain("{ScopeValue=\"InnerScopeValue\", Scope=[\"Outer Scope\", \"InnerScopeKey=InnerScopeValue\"], Application=\"Sample\"}");
        log.Should().Contain("Something bad.");
    }

    private sealed class TestDynamicMessageProcessor : IDynamicMessageProcessor
    {
        public string Process(string message)
        {
            return $"<<<[{message}]>>>";
        }
    }
}
