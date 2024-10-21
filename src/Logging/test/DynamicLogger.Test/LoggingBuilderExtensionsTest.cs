// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using A.B.C.D;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Logging.DynamicLogger.Test;

public sealed class LoggingBuilderExtensionsTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Logging:Console:IncludeScopes"] = "false",
        ["Logging:Console:LogLevel:Default"] = "Information",
        ["Logging:Console:LogLevel:A.B.C.D"] = "Critical",
        ["Logging:Console:FormatterOptions:ColorBehavior"] = "Disabled",
        ["Logging:LogLevel:Steeltoe.Logging.DynamicLogger.Test"] = "Information",
        ["Logging:LogLevel:Default"] = "Warning"
    };

    [Fact]
    public async Task OnlyApplicableFilters_AreApplied()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Default"] = "Information",
            ["Logging:foo:LogLevel:A.B.C.D.TestClass"] = "None"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
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
    public async Task DynamicLevelSetting_WorksWith_ConsoleFilters()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(AppSettings).Build();

        IServiceCollection services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
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
    public async Task AddConsole_Works_WithAddConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(AppSettings).Build();

        IServiceCollection services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
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
    public async Task AddDynamicConsole_Works_WithAddConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(AppSettings).Build();

        IServiceCollection services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
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
    public async Task DynamicLevelSetting_ParameterlessAddDynamic_NotBrokenByAddConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(AppSettings).Build();

        IServiceCollection services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
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

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(AppSettings).Build();

        IServiceCollection services = new ServiceCollection();
        services.AddSingleton<IDynamicMessageProcessor, TestDynamicMessageProcessor>();

        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var logger = serviceProvider.GetRequiredService<ILogger<LoggingBuilderExtensionsTest>>();
        logger.LogInformation("This is a test");

        // ConsoleLogger writes messages to a queue, it takes a bit of time for the background thread to write them to Console.Out.
        await Task.Delay(100);

        console.ToString().Should().Contain("TEST: This is a test");
    }

    [Fact]
    public async Task AddDynamicConsole_AddsAllLoggerProviders()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(AppSettings).Build();

        IServiceCollection services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var dynamicLoggerProvider = serviceProvider.GetService<IDynamicLoggerProvider>();
        dynamicLoggerProvider.Should().NotBeNull();

        ILoggerProvider[] loggerProviders = serviceProvider.GetServices<ILoggerProvider>().ToArray();
        loggerProviders.Should().HaveCount(1);
        loggerProviders[0].Should().BeOfType<DynamicConsoleLoggerProvider>();
    }

    [Fact]
    public void AddDynamicConsole_AddsLoggerProvider_DisposeTwiceSucceeds()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(AppSettings).Build();

        IServiceCollection services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var dynamicLoggerProvider = serviceProvider.GetRequiredService<IDynamicLoggerProvider>();

        serviceProvider.Dispose();

        Action action = dynamicLoggerProvider.Dispose;
        action.Should().NotThrow();
    }

    [Fact]
    public async Task DynamicLevelSetting_ParameterlessAddDynamic_AddsConsoleOptions()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(AppSettings).Build();

        IServiceCollection services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var formatterOptions = serviceProvider.GetRequiredService<IOptions<SimpleConsoleFormatterOptions>>();

        formatterOptions.Value.ColorBehavior.Should().Be(LoggerColorBehavior.Disabled);
    }

    [Fact]
    public async Task AddDynamicConsole_DoesNotSetColorLocal()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

        IServiceCollection services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var formatterOptions = serviceProvider.GetRequiredService<IOptionsMonitor<SimpleConsoleFormatterOptions>>();

        formatterOptions.CurrentValue.ColorBehavior.Should().NotBe(LoggerColorBehavior.Disabled);
    }

    [Fact]
    public async Task AddDynamicConsole_DisablesColorOnCloudFoundry()
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", "not empty");

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

        IServiceCollection services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var formatterOptions = serviceProvider.GetRequiredService<IOptionsMonitor<SimpleConsoleFormatterOptions>>();

        formatterOptions.CurrentValue.ColorBehavior.Should().Be(LoggerColorBehavior.Disabled);
    }

    [Fact]
    public async Task AddDynamicConsole_ObsoleteIncludesScopes()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Default"] = "Information",
            ["Logging:Console:IncludeScopes"] = "true",
            ["Logging:Console:DisableColors"] = "true"
        }).Build();

        IServiceCollection services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

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
        ((object)log).Should().BeEquivalentTo($"""
            fail: {typeof(LoggingBuilderExtensionsTest).FullName}[0]
                  => Outer Scope => InnerScopeKey=InnerScopeValue
                  Something bad.

            """, options => options.Using(IgnoreLineEndingsComparer.Instance));
    }

    [Fact]
    public async Task AddDynamicConsole_IncludesScopes()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Default"] = "Information",
            ["Logging:Console:FormatterName"] = "Simple",
            ["Logging:Console:FormatterOptions:IncludeScopes"] = "true",
            ["Logging:Console:FormatterOptions:ColorBehavior"] = "Disabled"
        }).Build();

        IServiceCollection services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

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
        ((object)log).Should().BeEquivalentTo($"""
            fail: {typeof(LoggingBuilderExtensionsTest).FullName}[0]
                  => Outer Scope => InnerScopeKey=InnerScopeValue
                  Something bad.

            """, options => options.Using(IgnoreLineEndingsComparer.Instance));
    }

    private sealed class TestDynamicMessageProcessor : IDynamicMessageProcessor
    {
        public string Process(string message)
        {
            return $"TEST: {message}";
        }
    }
}
