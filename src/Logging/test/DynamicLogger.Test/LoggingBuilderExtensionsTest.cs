// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using A.B.C.D;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Logging.DynamicLogger.Test;

public sealed class LoggingBuilderExtensionsTest
{
    private static readonly Dictionary<string, string?> Appsettings = new()
    {
        ["Logging:Console:IncludeScopes"] = "false",
        ["Logging:Console:LogLevel:Default"] = "Information",
        ["Logging:Console:LogLevel:A.B.C.D"] = "Critical",
        ["Logging:Console:FormatterOptions:ColorBehavior"] = "Disabled",
        ["Logging:LogLevel:Steeltoe.Logging.DynamicLogger.Test"] = "Information",
        ["Logging:LogLevel:Default"] = "Warning"
    };

    [Fact]
    public void OnlyApplicableFilters_AreApplied()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Default"] = "Information",
            ["Logging:foo:LogLevel:A.B.C.D.TestClass"] = "None"
        };

        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        ServiceProvider serviceProvider = new ServiceCollection().AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
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
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();

        ServiceProvider serviceProvider = new ServiceCollection().AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
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
        var provider = (DynamicConsoleLoggerProvider)serviceProvider.GetRequiredService<ILoggerProvider>();
        provider.SetLogLevel("A.B.C.D", LogLevel.Trace);

        logger.IsEnabled(LogLevel.Critical).Should().BeTrue();
        logger.IsEnabled(LogLevel.Error).Should().BeTrue();
        logger.IsEnabled(LogLevel.Warning).Should().BeTrue();
        logger.IsEnabled(LogLevel.Information).Should().BeTrue();
        logger.IsEnabled(LogLevel.Debug).Should().BeTrue();
        logger.IsEnabled(LogLevel.Trace).Should().BeTrue();
        logger.IsEnabled(LogLevel.None).Should().BeFalse();
    }

    [Fact]
    public void AddConsole_Works_WithAddConfiguration()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();

        ServiceProvider serviceProvider = new ServiceCollection().AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        }).BuildServiceProvider(true);

        var logger = serviceProvider.GetRequiredService<ILogger<LoggingBuilderExtensionsTest>>();

        logger.IsEnabled(LogLevel.Critical).Should().BeTrue();
        logger.IsEnabled(LogLevel.Error).Should().BeTrue();
        logger.IsEnabled(LogLevel.Warning).Should().BeTrue();
        logger.IsEnabled(LogLevel.Information).Should().BeTrue();
        logger.IsEnabled(LogLevel.Debug).Should().BeFalse();
        logger.IsEnabled(LogLevel.Trace).Should().BeFalse();
        logger.IsEnabled(LogLevel.None).Should().BeFalse();
    }

    [Fact]
    public void AddDynamicConsole_Works_WithAddConfiguration()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();

        ServiceProvider serviceProvider = new ServiceCollection().AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        }).BuildServiceProvider(true);

        var logger = serviceProvider.GetRequiredService<ILogger<LoggingBuilderExtensionsTest>>();

        logger.IsEnabled(LogLevel.Critical).Should().BeTrue();
        logger.IsEnabled(LogLevel.Error).Should().BeTrue();
        logger.IsEnabled(LogLevel.Warning).Should().BeTrue();
        logger.IsEnabled(LogLevel.Information).Should().BeTrue();
        logger.IsEnabled(LogLevel.Debug).Should().BeFalse();
        logger.IsEnabled(LogLevel.Trace).Should().BeFalse();
        logger.IsEnabled(LogLevel.None).Should().BeFalse();
    }

    [Fact]
    public void DynamicLevelSetting_ParameterlessAddDynamic_NotBrokenByAddConfiguration()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();

        ServiceProvider serviceProvider = new ServiceCollection().AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        }).BuildServiceProvider(true);

        var logger = serviceProvider.GetRequiredService<ILogger<LoggingBuilderExtensionsTest>>();

        logger.IsEnabled(LogLevel.Critical).Should().BeTrue();
        logger.IsEnabled(LogLevel.Error).Should().BeTrue();
        logger.IsEnabled(LogLevel.Warning).Should().BeTrue();
        logger.IsEnabled(LogLevel.Information).Should().BeTrue();
        logger.IsEnabled(LogLevel.Debug).Should().BeFalse();
        logger.IsEnabled(LogLevel.Trace).Should().BeFalse();
        logger.IsEnabled(LogLevel.None).Should().BeFalse();

        // change the log level and confirm it worked
        var provider = (DynamicConsoleLoggerProvider)serviceProvider.GetRequiredService<ILoggerProvider>();
        provider.SetLogLevel("Steeltoe.Logging.DynamicLogger.Test", LogLevel.Trace);

        logger.IsEnabled(LogLevel.Critical).Should().BeTrue();
        logger.IsEnabled(LogLevel.Error).Should().BeTrue();
        logger.IsEnabled(LogLevel.Warning).Should().BeTrue();
        logger.IsEnabled(LogLevel.Information).Should().BeTrue();
        logger.IsEnabled(LogLevel.Debug).Should().BeTrue();
        logger.IsEnabled(LogLevel.Trace).Should().BeTrue();
        logger.IsEnabled(LogLevel.None).Should().BeFalse();
    }

    [Fact]
    public async Task AddDynamicConsole_WithDynamicMessageProcessor_CallsProcessMessage()
    {
        using var console = new ConsoleOutputBorrower();

        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();

        ServiceProvider serviceProvider = new ServiceCollection().AddSingleton<IDynamicMessageProcessor, TestDynamicMessageProcessor>().AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        }).BuildServiceProvider(true);

        var logger = serviceProvider.GetRequiredService<ILogger<LoggingBuilderExtensionsTest>>();
        logger.LogInformation("This is a test");

        // ConsoleLogger writes messages to a queue, it takes a bit of time for the background thread to write them to Console.Out.
        await Task.Delay(100);

        console.ToString().Should().Contain("TEST: This is a test");
    }

    [Fact]
    public void AddDynamicConsole_AddsAllLoggerProviders()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();

        ServiceProvider serviceProvider = new ServiceCollection().AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        }).BuildServiceProvider(true);

        var dynamicLoggerProvider = serviceProvider.GetService<IDynamicLoggerProvider>();
        dynamicLoggerProvider.Should().NotBeNull();

        ILoggerProvider[] loggerProviders = serviceProvider.GetServices<ILoggerProvider>().ToArray();
        loggerProviders.Should().HaveCount(1);
        loggerProviders[0].Should().BeOfType<DynamicConsoleLoggerProvider>();
    }

    [Fact]
    public void AddDynamicConsole_AddsLoggerProvider_DisposeTwiceSucceeds()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();

        ServiceProvider serviceProvider = new ServiceCollection().AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        }).BuildServiceProvider(true);

        var dynamicLoggerProvider = serviceProvider.GetRequiredService<IDynamicLoggerProvider>();

        serviceProvider.Dispose();

        Action action = () => dynamicLoggerProvider.Dispose();
        action.Should().NotThrow();
    }

    [Fact]
    public void DynamicLevelSetting_ParameterlessAddDynamic_AddsConsoleOptions()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();

        ServiceProvider serviceProvider = new ServiceCollection().AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        }).BuildServiceProvider(true);

        var formatterOptions = serviceProvider.GetRequiredService<IOptions<SimpleConsoleFormatterOptions>>();

        formatterOptions.Value.ColorBehavior.Should().Be(LoggerColorBehavior.Disabled);
    }

    [Fact]
    public void AddDynamicConsole_DoesNotSetColorLocal()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

        ServiceProvider serviceProvider = new ServiceCollection().AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        }).BuildServiceProvider(true);

        using IServiceScope scope = serviceProvider.CreateScope();
        var formatterOptions = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<SimpleConsoleFormatterOptions>>();

        formatterOptions.Value.ColorBehavior.Should().NotBe(LoggerColorBehavior.Disabled);
    }

    [Fact]
    public void AddDynamicConsole_DisablesColorOnPivotalPlatform()
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", "not empty");

        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

        ServiceProvider serviceProvider = new ServiceCollection().AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        }).BuildServiceProvider(true);

        var formatterOptions = serviceProvider.GetRequiredService<IOptionsMonitor<SimpleConsoleFormatterOptions>>();

        formatterOptions.CurrentValue.ColorBehavior.Should().Be(LoggerColorBehavior.Disabled);
    }

    [Fact]
    public async Task AddDynamicConsole_ObsoleteIncludesScopes()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Default"] = "Information",
            ["Logging:Console:IncludeScopes"] = "true",
            ["Logging:Console:DisableColors"] = "true"
        }).Build();

        ServiceProvider serviceProvider = new ServiceCollection().AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        }).BuildServiceProvider(true);

        using var console = new ConsoleOutputBorrower();
        var logger = serviceProvider.GetRequiredService<ILogger<LoggingBuilderExtensionsTest>>();

        using (logger.BeginScope("Outer Scope"))
        {
            using (logger.BeginScope("InnerScopeKey={ScopeValue}", "InnerScopeValue"))
            {
                logger.LogError("Something bad.");
            }
        }

        // ConsoleLogger writes messages to a queue, it takes a bit of time for the background thread to write them to Console.Out.
        await Task.Delay(100);

        string log = console.ToString();

        // Casting to object as workaround, see https://github.com/fluentassertions/fluentassertions/issues/2339.
        ((object)log).Should().BeEquivalentTo($@"fail: {typeof(LoggingBuilderExtensionsTest).FullName}[0]
      => Outer Scope => InnerScopeKey=InnerScopeValue
      Something bad.
", options => options.Using(IgnoreLineEndingsComparer.Instance));
    }

    [Fact]
    public async Task AddDynamicConsole_IncludesScopes()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Default"] = "Information",
            ["Logging:Console:FormatterName"] = "Simple",
            ["Logging:Console:FormatterOptions:IncludeScopes"] = "true",
            ["Logging:Console:FormatterOptions:ColorBehavior"] = "Disabled"
        }).Build();

        ServiceProvider serviceProvider = new ServiceCollection().AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        }).BuildServiceProvider(true);

        using var console = new ConsoleOutputBorrower();
        var logger = serviceProvider.GetRequiredService<ILogger<LoggingBuilderExtensionsTest>>();

        using (logger.BeginScope("Outer Scope"))
        {
            using (logger.BeginScope("InnerScopeKey={ScopeValue}", "InnerScopeValue"))
            {
                logger.LogError("Something bad.");
            }
        }

        // ConsoleLogger writes messages to a queue, it takes a bit of time for the background thread to write them to Console.Out.
        await Task.Delay(100);

        string log = console.ToString();

        // Casting to object as workaround, see https://github.com/fluentassertions/fluentassertions/issues/2339.
        ((object)log).Should().BeEquivalentTo($@"fail: {typeof(LoggingBuilderExtensionsTest).FullName}[0]
      => Outer Scope => InnerScopeKey=InnerScopeValue
      Something bad.
", options => options.Using(IgnoreLineEndingsComparer.Instance));
    }

    private sealed class TestDynamicMessageProcessor : IDynamicMessageProcessor
    {
        public string Process(string message)
        {
            return $"TEST: {message}";
        }
    }
}
