// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Steeltoe.Logging.DynamicLogger.Test;

public sealed class DynamicConsoleLoggerProviderTest
{
    [Fact]
    public void Create_CreatesLoggerWithCorrectFilters()
    {
        var provider = CreateLoggerProvider<ILoggerProvider>(new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Fully.Qualified"] = "Warning"
        });

        ILogger logger = provider.CreateLogger("Fully.Qualified.Name.For.Type");

        logger.IsEnabled(LogLevel.Critical).Should().BeTrue();
        logger.IsEnabled(LogLevel.Error).Should().BeTrue();
        logger.IsEnabled(LogLevel.Warning).Should().BeTrue();
        logger.IsEnabled(LogLevel.Information).Should().BeFalse();
        logger.IsEnabled(LogLevel.Debug).Should().BeFalse();
        logger.IsEnabled(LogLevel.Trace).Should().BeFalse();
        logger.IsEnabled(LogLevel.None).Should().BeFalse();
    }

    [Fact]
    public void Create_FailsOnWildcardInConfiguration()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Some*Other"] = "Information"
        };

        Action action = () => CreateLoggerProvider<ILoggerProvider>(appSettings);

        action.Should().ThrowExactly<NotSupportedException>().WithMessage("Logger categories with wildcards are not supported.");
    }

    [Fact]
    public void GetLoggerConfigurations_ReturnsExpected()
    {
        var provider = CreateLoggerProvider<DynamicConsoleLoggerProvider>(new Dictionary<string, string?>
        {
            ["Logging:LogLevel:A"] = "Information"
        });

        _ = provider.CreateLogger("A.B.C.D.TestClass");

        string[] configurations = provider.GetLoggerConfigurations().Select(configuration => configuration.ToString()).ToArray();

        configurations.Should().HaveCount(6);
        configurations.Should().Contain("Default: Information -> Information");
        configurations.Should().Contain("A.B.C.D.TestClass: Information");
        configurations.Should().Contain("A.B.C.D: Information");
        configurations.Should().Contain("A.B.C: Information");
        configurations.Should().Contain("A.B: Information");
        configurations.Should().Contain("A: Information -> Information");
    }

    [Fact]
    public void GetLoggerConfigurations_UsesMinLevelInformationByDefault()
    {
        var provider = CreateLoggerProvider<DynamicConsoleLoggerProvider>();
        _ = provider.CreateLogger("Some");

        string[] configurations = provider.GetLoggerConfigurations().Select(configuration => configuration.ToString()).ToArray();

        configurations.Should().HaveCount(2);
        configurations.Should().Contain("Default: Information -> Information");
        configurations.Should().Contain("Some: Information");
    }

    [Fact]
    public void GetLoggerConfigurations_ReturnsExpectedAfterSetLogLevel()
    {
        var provider = CreateLoggerProvider<DynamicConsoleLoggerProvider>(new Dictionary<string, string?>
        {
            ["Logging:LogLevel:A"] = "Information"
        });

        _ = provider.CreateLogger("A.B.C.D.TestClass");

        string[] configurations = provider.GetLoggerConfigurations().Select(configuration => configuration.ToString()).ToArray();

        configurations.Should().HaveCount(6);
        configurations.Should().Contain("Default: Information -> Information");
        configurations.Should().Contain("A.B.C.D.TestClass: Information");
        configurations.Should().Contain("A.B.C.D: Information");
        configurations.Should().Contain("A.B.C: Information");
        configurations.Should().Contain("A.B: Information");
        configurations.Should().Contain("A: Information -> Information");

        provider.SetLogLevel("A.B", LogLevel.Trace);
        configurations = provider.GetLoggerConfigurations().Select(configuration => configuration.ToString()).ToArray();

        configurations.Should().HaveCount(6);
        configurations.Should().Contain("Default: Information -> Information");
        configurations.Should().Contain("A.B.C.D.TestClass: Trace");
        configurations.Should().Contain("A.B.C.D: Trace");
        configurations.Should().Contain("A.B.C: Trace");
        configurations.Should().Contain("A.B: Trace");
        configurations.Should().Contain("A: Information -> Information");
    }

    [Fact]
    public void SetLogLevel_UpdatesLogger()
    {
        var provider = CreateLoggerProvider<DynamicConsoleLoggerProvider>();

        ILogger logger = provider.CreateLogger("Fully.Qualified.Name.For.Type");

        provider.SetLogLevel("Fully", LogLevel.Debug);

        logger.IsEnabled(LogLevel.Critical).Should().BeTrue();
        logger.IsEnabled(LogLevel.Error).Should().BeTrue();
        logger.IsEnabled(LogLevel.Warning).Should().BeTrue();
        logger.IsEnabled(LogLevel.Information).Should().BeTrue();
        logger.IsEnabled(LogLevel.Debug).Should().BeTrue();
        logger.IsEnabled(LogLevel.Trace).Should().BeFalse();
        logger.IsEnabled(LogLevel.None).Should().BeFalse();

        provider.SetLogLevel("Fully.Qualified.Name", LogLevel.Warning);

        logger.IsEnabled(LogLevel.Critical).Should().BeTrue();
        logger.IsEnabled(LogLevel.Error).Should().BeTrue();
        logger.IsEnabled(LogLevel.Warning).Should().BeTrue();
        logger.IsEnabled(LogLevel.Information).Should().BeFalse();
        logger.IsEnabled(LogLevel.Debug).Should().BeFalse();
        logger.IsEnabled(LogLevel.Trace).Should().BeFalse();
        logger.IsEnabled(LogLevel.None).Should().BeFalse();
    }

    [Fact]
    public void SetLogLevel_UpdatesNamespaceDescendants()
    {
        // arrange (A* should log at Information)
        var provider = CreateLoggerProvider<DynamicConsoleLoggerProvider>();

        // act I: with original setup
        ILogger childLogger = provider.CreateLogger("A.B.C");
        ICollection<DynamicLoggerConfiguration> configurations = provider.GetLoggerConfigurations();
        DynamicLoggerConfiguration tierOneNamespace = configurations.First(configuration => configuration.CategoryName == "A");

        // assert I: base namespace is in the response, correctly
        tierOneNamespace.EffectiveMinLevel.Should().Be(LogLevel.Information);

        // act II: set A.B* to log at Trace
        provider.SetLogLevel("A.B", LogLevel.Trace);
        configurations = provider.GetLoggerConfigurations();
        tierOneNamespace = configurations.First(configuration => configuration.CategoryName == "A");
        DynamicLoggerConfiguration tierTwoNamespace = configurations.First(configuration => configuration.CategoryName == "A.B");

        // assert II: base hasn't changed but the one set at runtime and all descendants (including a concrete logger) have
        tierOneNamespace.EffectiveMinLevel.Should().Be(LogLevel.Information);
        tierTwoNamespace.EffectiveMinLevel.Should().Be(LogLevel.Trace);
        childLogger.IsEnabled(LogLevel.Trace).Should().BeTrue();

        // act III: set A to something else, make sure it inherits down
        provider.SetLogLevel("A", LogLevel.Error);
        configurations = provider.GetLoggerConfigurations();
        tierOneNamespace = configurations.First(configuration => configuration.CategoryName == "A");
        tierTwoNamespace = configurations.First(configuration => configuration.CategoryName == "A.B");
        ILogger grandchildLogger = provider.CreateLogger("A.B.C.D");

        // assert III
        tierOneNamespace.EffectiveMinLevel.Should().Be(LogLevel.Error);
        tierTwoNamespace.EffectiveMinLevel.Should().Be(LogLevel.Error);
        childLogger.IsEnabled(LogLevel.Warning).Should().BeFalse();
        grandchildLogger.IsEnabled(LogLevel.Warning).Should().BeFalse();
    }

    [Fact]
    public void SetLogLevel_DoesNotUpdateUnrelatedCategoriesWithSamePrefix()
    {
        var provider = CreateLoggerProvider<DynamicConsoleLoggerProvider>(new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Default"] = "Warning",
            ["Logging:LogLevel:Example"] = "Critical"
        });

        _ = provider.CreateLogger("Example");
        _ = provider.CreateLogger("Example.Some");
        _ = provider.CreateLogger("ExampleWithSuffix");
        _ = provider.CreateLogger("ExampleWithSuffix.Other");

        provider.SetLogLevel("Example", LogLevel.Error);

        string[] configurations = provider.GetLoggerConfigurations().Select(configuration => configuration.ToString()).ToArray();

        configurations.Should().HaveCount(5);
        configurations.Should().Contain("Default: Warning -> Warning");
        configurations.Should().Contain("Example: Critical -> Error");
        configurations.Should().Contain("Example.Some: Error");
        configurations.Should().Contain("ExampleWithSuffix: Warning");
        configurations.Should().Contain("ExampleWithSuffix.Other: Warning");
    }

    [Fact]
    public void SetLogLevel_CanResetToDefault()
    {
        // arrange (A* should log at Information)
        var provider = CreateLoggerProvider<DynamicConsoleLoggerProvider>(new Dictionary<string, string?>
        {
            ["Logging:LogLevel:A"] = "Information"
        });

        // act I: with original setup
        ILogger firstLogger = provider.CreateLogger("A.B.C");
        ICollection<DynamicLoggerConfiguration> configurations = provider.GetLoggerConfigurations();
        DynamicLoggerConfiguration tierOneNamespace = configurations.First(configuration => configuration.CategoryName == "A");

        // assert I: base namespace is in the response, correctly
        tierOneNamespace.EffectiveMinLevel.Should().Be(LogLevel.Information);

        // act II: set A.B* to log at Trace
        provider.SetLogLevel("A.B", LogLevel.Trace);
        configurations = provider.GetLoggerConfigurations();
        tierOneNamespace = configurations.First(configuration => configuration.CategoryName == "A");
        DynamicLoggerConfiguration tierTwoNamespace = configurations.First(configuration => configuration.CategoryName == "A.B");

        // assert II: base hasn't changed but the one set at runtime and all descendants (including a concrete logger) have
        tierOneNamespace.EffectiveMinLevel.Should().Be(LogLevel.Information);
        tierTwoNamespace.EffectiveMinLevel.Should().Be(LogLevel.Trace);
        firstLogger.IsEnabled(LogLevel.Trace).Should().BeTrue();

        // act III: reset A.B
        provider.SetLogLevel("A.B", null);
        configurations = provider.GetLoggerConfigurations();
        tierOneNamespace = configurations.First(configuration => configuration.CategoryName == "A");
        tierTwoNamespace = configurations.First(configuration => configuration.CategoryName == "A.B");
        ILogger secondLogger = provider.CreateLogger("A.B.C.D");

        // assert again
        tierOneNamespace.EffectiveMinLevel.Should().Be(LogLevel.Information);
        tierTwoNamespace.EffectiveMinLevel.Should().Be(LogLevel.Information);
        firstLogger.IsEnabled(LogLevel.Information).Should().BeTrue();
        secondLogger.IsEnabled(LogLevel.Information).Should().BeTrue();
    }

    [Fact]
    public void SetLogLevel_WorksOnDefault()
    {
        var provider = CreateLoggerProvider<DynamicConsoleLoggerProvider>();
        string[] originalConfiguration = provider.GetLoggerConfigurations().Select(configuration => configuration.ToString()).ToArray();

        provider.SetLogLevel("Default", LogLevel.Trace);
        string[] updatedConfiguration = provider.GetLoggerConfigurations().Select(configuration => configuration.ToString()).ToArray();

        originalConfiguration.Should().Contain("Default: Information -> Information");
        updatedConfiguration.Should().Contain("Default: Information -> Trace");
    }

    [Fact]
    public void ResetLogLevel_WorksOnDefault()
    {
        var provider = CreateLoggerProvider<DynamicConsoleLoggerProvider>(new Dictionary<string, string?>
        {
            ["Logging:LogLevel:A"] = "Information"
        });

        string[] originalConfiguration = provider.GetLoggerConfigurations().Select(configuration => configuration.ToString()).ToArray();

        provider.SetLogLevel("Default", LogLevel.Debug);
        string[] updatedConfiguration = provider.GetLoggerConfigurations().Select(configuration => configuration.ToString()).ToArray();
        provider.SetLogLevel("Default", null);
        string[] resetConfiguration = provider.GetLoggerConfigurations().Select(configuration => configuration.ToString()).ToArray();

        originalConfiguration.Should().Contain("Default: Information -> Information");
        updatedConfiguration.Should().Contain("Default: Information -> Debug");
        resetConfiguration.Should().Contain("Default: Information -> Information");
    }

    [Fact]
    public async Task Logger_LogsAtConfiguredSetting()
    {
        using var console = new ConsoleOutputBorrower();

        var provider = CreateLoggerProvider<DynamicConsoleLoggerProvider>(new Dictionary<string, string?>
        {
            ["Logging:LogLevel:A"] = "Information"
        });

        ILogger logger = provider.CreateLogger("A.B.C.D.TestClass");

        // act I - log at all levels, expect Info and above to work
        await WriteLogEntriesAsync(logger);
        string logged1 = console.ToString();

        // assert I
        logged1.Should().Contain("Critical message");
        logged1.Should().Contain("Error message");
        logged1.Should().Contain("Warning message");
        logged1.Should().Contain("Informational message");
        logged1.Should().NotContain("Debug message");
        logged1.Should().NotContain("Trace message");

        // act II - adjust rules, expect Error and above to work
        provider.SetLogLevel("A.B.C.D", LogLevel.Error);
        console.Clear();

        await WriteLogEntriesAsync(logger);
        string logged2 = console.ToString();

        // assert II
        logged2.Should().Contain("Critical message");
        logged2.Should().Contain("Error message");
        logged2.Should().NotContain("Warning message");
        logged2.Should().NotContain("Informational message");
        logged2.Should().NotContain("Debug message");
        logged2.Should().NotContain("Trace message");

        // act III - adjust rules, expect Trace and above to work
        provider.SetLogLevel("A", LogLevel.Trace);
        console.Clear();

        await WriteLogEntriesAsync(logger);
        string logged3 = console.ToString();

        // assert III
        logged3.Should().Contain("Critical message");
        logged3.Should().Contain("Error message");
        logged3.Should().Contain("Warning message");
        logged3.Should().Contain("Informational message");
        logged3.Should().Contain("Debug message");
        logged3.Should().Contain("Trace message");

        // act IV - adjust rules, expect nothing to work
        provider.SetLogLevel("A", LogLevel.None);
        console.Clear();

        await WriteLogEntriesAsync(logger);
        string logged4 = console.ToString();

        // assert IV
        logged4.Should().NotContain("Critical message");
        logged4.Should().NotContain("Error message");
        logged4.Should().NotContain("Warning message");
        logged4.Should().NotContain("Informational message");
        logged4.Should().NotContain("Debug message");
        logged4.Should().NotContain("Trace message");

        // act V - reset the rules, expect Info and above to work
        provider.SetLogLevel("A", null);
        console.Clear();

        await WriteLogEntriesAsync(logger);
        string logged5 = console.ToString();

        // assert V
        logged5.Should().Contain("Critical message");
        logged5.Should().Contain("Error message");
        logged5.Should().Contain("Warning message");
        logged5.Should().Contain("Informational message");
        logged5.Should().NotContain("Debug message");
        logged5.Should().NotContain("Trace message");
    }

    [Fact]
    public async Task Logger_CategoryIsCaseSensitive()
    {
        using var console = new ConsoleOutputBorrower();

        var provider = CreateLoggerProvider<DynamicConsoleLoggerProvider>(new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Some"] = "Critical"
        });

        ILogger logger = provider.CreateLogger("Some");

        provider.SetLogLevel("SOME", LogLevel.Warning);

        await WriteLogEntriesAsync(logger);
        string logged = console.ToString();

        logged.Should().Contain("Critical message");
        logged.Should().NotContain("Error message");
        logged.Should().NotContain("Warning message");
        logged.Should().NotContain("Informational message");
        logged.Should().NotContain("Debug message");
        logged.Should().NotContain("Trace message");
    }

    private static async Task WriteLogEntriesAsync(ILogger logger)
    {
        logger.LogCritical("Critical message");
        logger.LogError("Error message");
        logger.LogWarning("Warning message");
        logger.LogInformation("Informational message");
        logger.LogDebug("Debug message");
        logger.LogTrace("Trace message");

        // ConsoleLogger writes messages to a queue, it takes a bit of time for the background thread to write them to Console.Out.
        await Task.Delay(100);
    }

    private static TLoggerProvider CreateLoggerProvider<TLoggerProvider>(Dictionary<string, string?>? appSettings = null)
        where TLoggerProvider : ILoggerProvider
    {
        var configurationBuilder = new ConfigurationBuilder();

        if (appSettings != null)
        {
            configurationBuilder.AddInMemoryCollection(appSettings);
        }

        IConfigurationRoot configuration = configurationBuilder.Build();

        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        return (TLoggerProvider)serviceProvider.GetRequiredService<ILoggerProvider>();
    }
}
