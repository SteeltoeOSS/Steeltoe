// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using A.B.C.D;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Logging.DynamicSerilog.Test;

public sealed class DynamicSerilogLoggerProviderTest
{
    public DynamicSerilogLoggerProviderTest()
    {
        DynamicSerilogLoggerProvider.ClearLogger();
    }

    [Fact]
    public void Create_CreatesLoggerWithCorrectFilters()
    {
        var provider = new DynamicSerilogLoggerProvider(GetConfiguration(), Enumerable.Empty<IDynamicMessageProcessor>());
        var factory = new LoggerFactory();
        factory.AddProvider(provider);

        ILogger logger = factory.CreateLogger(typeof(TestClass));

        logger.IsEnabled(LogLevel.Critical).Should().BeTrue();
        logger.IsEnabled(LogLevel.Error).Should().BeTrue();
        logger.IsEnabled(LogLevel.Warning).Should().BeTrue();
        logger.IsEnabled(LogLevel.Information).Should().BeTrue();
        logger.IsEnabled(LogLevel.Debug).Should().BeFalse();
        logger.IsEnabled(LogLevel.Trace).Should().BeFalse();
        logger.IsEnabled(LogLevel.None).Should().BeFalse();
    }

    [Fact]
    public void GetLoggerConfigurations_ReturnsExpected()
    {
        var provider = new DynamicSerilogLoggerProvider(GetConfiguration(), Enumerable.Empty<IDynamicMessageProcessor>());
        var factory = new LoggerFactory();
        factory.AddProvider(provider);

        _ = factory.CreateLogger(typeof(TestClass));

        string[] configurations = provider.GetLoggerConfigurations().Select(configuration => configuration.ToString()).ToArray();

        configurations.Should().HaveCount(6);
        configurations.Should().Contain("Default: Information -> Information");
        configurations.Should().Contain("A.B.C.D.TestClass: Information");
        configurations.Should().Contain("A.B.C.D: Information");
        configurations.Should().Contain("A.B.C: Information -> Information");
        configurations.Should().Contain("A.B: Information");
        configurations.Should().Contain("A: Information -> Information");
    }

    [Fact]
    public void GetLoggerConfigurations_UsesMinLevelInformationByDefault()
    {
        var provider = new DynamicSerilogLoggerProvider(GetConfiguration(), Enumerable.Empty<IDynamicMessageProcessor>());
        var factory = new LoggerFactory();
        factory.AddProvider(provider);

        _ = factory.CreateLogger(typeof(TestClass));

        string[] configurations = provider.GetLoggerConfigurations().Select(configuration => configuration.ToString()).ToArray();

        configurations.Should().Contain("Default: Information -> Information");
        configurations.Should().Contain("A.B.C.D.TestClass: Information");
    }

    [Fact]
    public void GetLoggerConfigurations_ReturnsExpectedAfterSetLogLevel()
    {
        var provider = new DynamicSerilogLoggerProvider(GetConfiguration(), Enumerable.Empty<IDynamicMessageProcessor>());
        var factory = new LoggerFactory();
        factory.AddProvider(provider);

        factory.CreateLogger(typeof(TestClass));

        string[] configurations = provider.GetLoggerConfigurations().Select(configuration => configuration.ToString()).ToArray();

        configurations.Should().HaveCount(6);
        configurations.Should().Contain("Default: Information -> Information");
        configurations.Should().Contain("A.B.C.D.TestClass: Information");
        configurations.Should().Contain("A.B.C.D: Information");
        configurations.Should().Contain("A.B.C: Information -> Information");
        configurations.Should().Contain("A.B: Information");
        configurations.Should().Contain("A: Information -> Information");

        provider.SetLogLevel("A.B", LogLevel.Trace);
        configurations = provider.GetLoggerConfigurations().Select(configuration => configuration.ToString()).ToArray();

        configurations.Should().HaveCount(6);
        configurations.Should().Contain("Default: Information -> Information");
        configurations.Should().Contain("A.B.C.D.TestClass: Trace");
        configurations.Should().Contain("A.B.C.D: Trace");
        configurations.Should().Contain("A.B.C: Information -> Trace");
        configurations.Should().Contain("A.B: Trace");
        configurations.Should().Contain("A: Information -> Information");
    }

    [Fact]
    public void SetLogLevel_UpdatesLogger()
    {
        var provider = new DynamicSerilogLoggerProvider(GetConfiguration(), Enumerable.Empty<IDynamicMessageProcessor>());
        var factory = new LoggerFactory();
        factory.AddProvider(provider);

        ILogger logger = factory.CreateLogger(typeof(TestClass));

        provider.SetLogLevel("A", LogLevel.Debug);

        logger.IsEnabled(LogLevel.Critical).Should().BeTrue();
        logger.IsEnabled(LogLevel.Error).Should().BeTrue();
        logger.IsEnabled(LogLevel.Warning).Should().BeTrue();
        logger.IsEnabled(LogLevel.Information).Should().BeTrue();
        logger.IsEnabled(LogLevel.Debug).Should().BeTrue();
        logger.IsEnabled(LogLevel.Trace).Should().BeFalse();
        logger.IsEnabled(LogLevel.None).Should().BeFalse();

        provider.SetLogLevel("A", LogLevel.Warning);

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
        var provider = new DynamicSerilogLoggerProvider(GetConfiguration(), Enumerable.Empty<IDynamicMessageProcessor>());

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
    public void SetLogLevel_CanResetToDefault()
    {
        // arrange (A* should log at Information)
        var provider = new DynamicSerilogLoggerProvider(GetConfiguration(), Enumerable.Empty<IDynamicMessageProcessor>());

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
        var provider = new DynamicSerilogLoggerProvider(GetConfiguration(), Enumerable.Empty<IDynamicMessageProcessor>());
        var factory = new LoggerFactory();
        factory.AddProvider(provider);

        string[] originalConfiguration = provider.GetLoggerConfigurations().Select(configuration => configuration.ToString()).ToArray();

        provider.SetLogLevel("Default", LogLevel.Trace);
        string[] updatedConfiguration = provider.GetLoggerConfigurations().Select(configuration => configuration.ToString()).ToArray();

        originalConfiguration.Should().Contain("Default: Information -> Information");
        updatedConfiguration.Should().Contain("Default: Information -> Trace");
    }

    [Fact]
    public void ResetLogLevel_WorksOnDefault()
    {
        var provider = new DynamicSerilogLoggerProvider(GetConfiguration(), Enumerable.Empty<IDynamicMessageProcessor>());
        var factory = new LoggerFactory();
        factory.AddProvider(provider);

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
    public void Logger_LogsWithEnrichers()
    {
        using var console = new ConsoleOutputBorrower();
        var provider = new DynamicSerilogLoggerProvider(GetConfigurationFromFile(), Enumerable.Empty<IDynamicMessageProcessor>());
        var factory = new LoggerFactory();
        factory.AddProvider(provider);
        ILogger logger = factory.CreateLogger(typeof(TestClass));

        using (LogContext.PushProperty("A", 1))
        {
            logger.LogInformation("Carries property A = 1");

            using (LogContext.PushProperty("A", 2))
            {
                using (LogContext.PushProperty("B", 1))
                {
                    logger.LogInformation("Carries A = 2 and B = 1");
                }
            }

            logger.LogInformation("Carries property A = 1, again");
        }

        string logged = console.ToString();

        logged.Should().Contain(@"A.B.C.D.TestClass: {A=1, Application=""Sample""}");
        logged.Should().Contain("Carries property A = 1");
        logged.Should().Contain(@"A.B.C.D.TestClass: {B=1, A=2, Application=""Sample""}");
        logged.Should().Contain("Carries A = 2 and B = 1");
        logged.Should().Contain(@"A.B.C.D.TestClass: {A=1, Application=""Sample""}");
        logged.Should().Contain("Carries property A = 1, again");
        logged.Should().MatchRegex(new Regex(@"ThreadId:<\d+>"));
    }

    [Fact]
    public void Logger_LogsWithDestructuring()
    {
        using var console = new ConsoleOutputBorrower();
        var provider = new DynamicSerilogLoggerProvider(GetConfigurationFromFile(), Enumerable.Empty<IDynamicMessageProcessor>());
        var factory = new LoggerFactory();
        factory.AddProvider(provider);
        ILogger logger = factory.CreateLogger(typeof(TestClass));

        logger.LogInformation("Info {@TestInfo}", new
        {
            Info1 = "information1",
            Info2 = "information2"
        });

        string logged = console.ToString();

        logged.Should().Contain("Info {\"Info1\": \"information1\", \"Info2\": \"information2\"}");
    }

    [Fact]
    public void Logger_LogsAtConfiguredSetting()
    {
        using var console = new ConsoleOutputBorrower();
        var provider = new DynamicSerilogLoggerProvider(GetConfiguration(), Enumerable.Empty<IDynamicMessageProcessor>());
        var factory = new LoggerFactory();
        factory.AddProvider(provider);
        ILogger logger = factory.CreateLogger(typeof(TestClass));

        // act I - log at all levels, expect Info and above to work
        WriteLogEntries(logger);
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

        WriteLogEntries(logger);
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

        WriteLogEntries(logger);
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

        WriteLogEntries(logger);
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

        WriteLogEntries(logger);
        string logged5 = console.ToString();

        // assert V
        logged5.Should().Contain("Critical message");
        logged5.Should().Contain("Error message");
        logged5.Should().Contain("Warning message");
        logged5.Should().Contain("Informational message");
        logged5.Should().NotContain("Debug message");
        logged5.Should().NotContain("Trace message");
    }

    private void WriteLogEntries(ILogger logger)
    {
        logger.LogCritical("Critical message");
        logger.LogError("Error message");
        logger.LogWarning("Warning message");
        logger.LogInformation("Informational message");
        logger.LogDebug("Debug message");
        logger.LogTrace("Trace message");
    }

    private IOptionsMonitor<SerilogOptions> GetConfiguration()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "Serilog:MinimumLevel:Default", "Information" },
            { "Serilog:MinimumLevel:Override:Microsoft", "Warning" },
            { "Serilog:MinimumLevel:Override:Steeltoe.Extensions", "Verbose" },
            { "Serilog:MinimumLevel:Override:Steeltoe", "Information" },
            { "Serilog:MinimumLevel:Override:A", "Information" },
            { "Serilog:MinimumLevel:Override:A.B.C", "Information" },
            { "Serilog:WriteTo:Name", "Console" }
        };

        IConfigurationBuilder builder = new ConfigurationBuilder().AddInMemoryCollection(appSettings);
        IConfigurationRoot configuration = builder.Build();

        var serilogOptions = new SerilogOptions();
        serilogOptions.SetSerilogOptions(configuration);
        return new TestOptionsMonitor<SerilogOptions>(serilogOptions);
    }

    private IOptionsMonitor<SerilogOptions> GetConfigurationFromFile()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("serilogSettings.json");
        IConfigurationRoot configuration = builder.Build();
        var serilogOptions = new SerilogOptions();
        serilogOptions.SetSerilogOptions(configuration);
        return new TestOptionsMonitor<SerilogOptions>(serilogOptions);
    }
}
