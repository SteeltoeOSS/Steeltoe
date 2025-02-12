// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Logging.DynamicSerilog.Test;

public sealed class DynamicSerilogLoggerProviderTest : IDisposable
{
    private readonly ConsoleOutput _consoleOutput = ConsoleOutput.Capture();

    public DynamicSerilogLoggerProviderTest()
    {
        DynamicSerilogLoggerProvider.ClearLogger();
    }

    [Fact]
    public void CreatesLoggerWithCorrectFilters()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Serilog:MinimumLevel:Override:Fully.Qualified"] = DynamicLoggingTestContext.ToSerilogLevel(LogLevel.Warning)
        };

        using IDynamicLoggerProvider provider = CreateLoggerProvider(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
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
    public void GetLogLevelsReturnsIntermediateCategories()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Serilog:MinimumLevel:Override:A"] = DynamicLoggingTestContext.ToSerilogLevel(LogLevel.Trace)
        };

        using IDynamicLoggerProvider provider = CreateLoggerProvider(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
        _ = provider.CreateLogger("A.B.C.D.Example");

        string[] loggerStates = [.. provider.GetLogLevels().Select(state => state.ToString())];

        loggerStates.Should().HaveCount(6);
        loggerStates.Should().Contain("Default: Information");
        loggerStates.Should().Contain("A: Trace");
        loggerStates.Should().Contain("A.B: Trace");
        loggerStates.Should().Contain("A.B.C: Trace");
        loggerStates.Should().Contain("A.B.C.D: Trace");
        loggerStates.Should().Contain("A.B.C.D.Example: Trace");
    }

    [Theory]
    [InlineData(ConfigurationCategory.Parent, LogLevel.Error, LogLevel.Trace)]
    [InlineData(ConfigurationCategory.Parent, LogLevel.Trace, LogLevel.Error)]
    [InlineData(ConfigurationCategory.Parent, LogLevel.Debug, LogLevel.Debug)]
    [InlineData(ConfigurationCategory.Self, LogLevel.Error, LogLevel.Trace)]
    [InlineData(ConfigurationCategory.Self, LogLevel.Trace, LogLevel.Error)]
    [InlineData(ConfigurationCategory.Self, LogLevel.Debug, LogLevel.Debug)]
    [InlineData(ConfigurationCategory.Child, LogLevel.Error, LogLevel.Trace)]
    [InlineData(ConfigurationCategory.Child, LogLevel.Trace, LogLevel.Error)]
    [InlineData(ConfigurationCategory.Child, LogLevel.Debug, LogLevel.Debug)]
    public void CanSetAndResetMinLevel(ConfigurationCategory configurationCategory, LogLevel configurationLevel, LogLevel overrideLevel)
    {
        string configurationKey = configurationCategory switch
        {
            ConfigurationCategory.Parent => "A",
            ConfigurationCategory.Self => "A.B",
            ConfigurationCategory.Child => "A.B.C",
            _ => throw new ArgumentOutOfRangeException(nameof(configurationCategory))
        };

        var appSettings = new Dictionary<string, string?>
        {
            [$"Serilog:MinimumLevel:Override:{configurationKey}"] = DynamicLoggingTestContext.ToSerilogLevel(configurationLevel),
            ["Serilog:WriteTo:0:Name"] = "Console"
        };

        LogLevel expectBeforeLevelAtParent = configurationCategory == ConfigurationCategory.Parent ? configurationLevel : LogLevel.Information;
        LogLevel expectBeforeLevelAtSelf = configurationCategory != ConfigurationCategory.Child ? configurationLevel : LogLevel.Information;
        LogLevel expectBeforeLevelAtChild = configurationLevel;

        using IDynamicLoggerProvider provider = CreateLoggerProvider(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
        DynamicLoggingTestContext testContext = new(provider, _consoleOutput);

        testContext.Parent.AssertMinLevel(expectBeforeLevelAtParent);
        testContext.Self.AssertMinLevel(expectBeforeLevelAtSelf);
        testContext.Child.AssertMinLevel(expectBeforeLevelAtChild);

        provider.SetLogLevel(testContext.Self.CategoryName, overrideLevel);
        testContext.Refresh();

        testContext.Parent.AssertMinLevel(expectBeforeLevelAtParent);
        testContext.Self.AssertMinLevel(overrideLevel, expectBeforeLevelAtSelf);
        testContext.Child.AssertMinLevel(overrideLevel);

        provider.SetLogLevel(testContext.Self.CategoryName, null);
        testContext.Refresh();

        testContext.Parent.AssertMinLevel(expectBeforeLevelAtParent);
        testContext.Self.AssertMinLevel(expectBeforeLevelAtSelf);
        testContext.Child.AssertMinLevel(expectBeforeLevelAtChild);
    }

    [Fact]
    public void CanSetAndResetImplicitDefaultMinLevel()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Serilog:WriteTo:0:Name"] = "Console"
        };

        using IDynamicLoggerProvider provider = CreateLoggerProvider(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
        DynamicLoggingTestContext testContext = new(provider, _consoleOutput);

        testContext.Default.AssertMinLevel(LogLevel.Information);
        testContext.Self.AssertMinLevel(LogLevel.Information);

        provider.SetLogLevel(string.Empty, LogLevel.Error);
        testContext.Refresh();

        testContext.Default.AssertMinLevel(LogLevel.Error, LogLevel.Information);
        testContext.Self.AssertMinLevel(LogLevel.Error);

        provider.SetLogLevel(string.Empty, null);
        testContext.Refresh();

        testContext.Default.AssertMinLevel(LogLevel.Information);
        testContext.Self.AssertMinLevel(LogLevel.Information);
    }

    [Fact]
    public void SetIsAppliedToBothExistingAndNewLogger()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Serilog:MinimumLevel:Override:Some"] = DynamicLoggingTestContext.ToSerilogLevel(LogLevel.Trace)
        };

        using IDynamicLoggerProvider provider = CreateLoggerProvider(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));

        ILogger beforeLogger = provider.CreateLogger("Some");
        beforeLogger.ProbeMinLevel().Should().Be(LogLevel.Trace);

        provider.SetLogLevel("Some", LogLevel.Error);
        ILogger afterLogger = provider.CreateLogger("Some.Other");

        beforeLogger.ProbeMinLevel().Should().Be(LogLevel.Error);
        afterLogger.ProbeMinLevel().Should().Be(LogLevel.Error);
    }

    [Fact]
    public void CategoriesAreCaseSensitive()
    {
        const string pascalCaseCategoryName = "Some";
        const string upperCaseCategoryName = "SOME";

        var appSettings = new Dictionary<string, string?>
        {
            [$"Serilog:MinimumLevel:Override:{pascalCaseCategoryName}"] = DynamicLoggingTestContext.ToSerilogLevel(LogLevel.Critical),
            ["Serilog:WriteTo:0:Name"] = "Console"
        };

        using IDynamicLoggerProvider provider = CreateLoggerProvider(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
        LoggerWithDynamicState pascalCaseState = new(provider, _consoleOutput, pascalCaseCategoryName);
        LoggerWithDynamicState upperCaseState = new(provider, _consoleOutput, upperCaseCategoryName);

        provider.SetLogLevel(pascalCaseCategoryName, LogLevel.Trace);
        Refresh();

        pascalCaseState.AssertMinLevel(LogLevel.Trace, LogLevel.Critical);
        upperCaseState.AssertMinLevel(LogLevel.Information);

        void Refresh()
        {
            ICollection<DynamicLoggerState> logLevels = provider.GetLogLevels();

            pascalCaseState.Refresh(logLevels);
            upperCaseState.Refresh(logLevels);
        }
    }

    [Fact]
    public void AppliesChangedConfiguration()
    {
        const string fileName = "appsettings.json";
        MemoryFileProvider fileProvider = new();

        fileProvider.IncludeFile(fileName, """
        {
          "Serilog": {
            "MinimumLevel": {
              "Override": {
                "A": "Warning"
              }
            },
            "WriteTo": {
              "Name": "Console"
            }
          }
        }
        """);

        using IDynamicLoggerProvider provider =
            CreateLoggerProvider(configurationBuilder => configurationBuilder.AddJsonFile(fileProvider, fileName, false, true));

        DynamicLoggingTestContext testContext = new(provider, _consoleOutput);

        testContext.Parent.AssertMinLevel(LogLevel.Warning);
        testContext.Self.AssertMinLevel(LogLevel.Warning);
        testContext.Child.AssertMinLevel(LogLevel.Warning);

        provider.SetLogLevel(testContext.Self.CategoryName, LogLevel.Error);
        testContext.Refresh();

        testContext.Parent.AssertMinLevel(LogLevel.Warning);
        testContext.Self.AssertMinLevel(LogLevel.Error, LogLevel.Warning);
        testContext.Child.AssertMinLevel(LogLevel.Error);

        fileProvider.ReplaceFile(fileName, """
        {
          "Serilog": {
            "MinimumLevel": {
              "Override": {
                "A": "Verbose",
                "A.B.C": "Debug"
              }
            },
            "WriteTo": "Console"
          }
        }
        """);

        fileProvider.NotifyChanged();
        testContext.Refresh();

        testContext.Parent.AssertMinLevel(LogLevel.Trace);
        testContext.Self.AssertMinLevel(LogLevel.Error, LogLevel.Trace);
        testContext.Child.AssertMinLevel(LogLevel.Error);

        provider.SetLogLevel(testContext.Self.CategoryName, null);
        testContext.Refresh();

        testContext.Parent.AssertMinLevel(LogLevel.Trace);
        testContext.Self.AssertMinLevel(LogLevel.Trace);
        testContext.Child.AssertMinLevel(LogLevel.Debug);
    }

    [Fact]
    public void CanUseScopes()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Serilog:WriteTo:0:Name"] = "Console",
            ["Serilog:WriteTo:0:Args:OutputTemplate"] = "[{Level:u3}] {SourceContext}: {Properties}{NewLine}  {Message:lj}{NewLine}"
        };

        using IDynamicLoggerProvider provider = CreateLoggerProvider(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));

        using var factory = new LoggerFactory();
        factory.AddProvider(provider);
        ILogger logger = factory.CreateLogger("Fully.Qualified.Type");

        using (logger.BeginScope("OuterScope"))
        {
            using (logger.BeginScope("InnerScope={InnerScopeKey}", "InnerScopeValue"))
            {
                logger.LogInformation("TestInfo");
            }
        }

        string logOutput = _consoleOutput.ToString();

        logOutput.Should().Be("""
            [INF] Fully.Qualified.Type: {InnerScopeKey="InnerScopeValue", Scope=["OuterScope", "InnerScope=InnerScopeValue"]}
              TestInfo

            """);
    }

    [Fact]
    public void CanUseSerilogEnrichers()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Serilog:WriteTo:0:Name"] = "Console",
            ["Serilog:WriteTo:0:Args:OutputTemplate"] = "[{Level:u3}] {SourceContext}: {Properties}{NewLine}  {Message:lj}{NewLine}",
            ["Serilog:Enrich:0"] = "FromLogContext"
        };

        using IDynamicLoggerProvider provider = CreateLoggerProvider(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));

        using var factory = new LoggerFactory();
        factory.AddProvider(provider);
        ILogger logger = factory.CreateLogger("Fully.Qualified.Type");

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

        string logOutput = _consoleOutput.ToString();

        logOutput.Should().Be("""
            [INF] Fully.Qualified.Type: {A=1}
              Carries property A = 1
            [INF] Fully.Qualified.Type: {B=1, A=2}
              Carries A = 2 and B = 1
            [INF] Fully.Qualified.Type: {A=1}
              Carries property A = 1, again

            """);
    }

    [Fact]
    public void CanUseSerilogDestructuring()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Serilog:WriteTo:0:Name"] = "Console",
            ["Serilog:WriteTo:0:Args:OutputTemplate"] = "[{Level:u3}] {SourceContext}: {Message:lj}{NewLine}"
        };

        using IDynamicLoggerProvider provider = CreateLoggerProvider(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));

        using var factory = new LoggerFactory();
        factory.AddProvider(provider);
        ILogger logger = factory.CreateLogger("Fully.Qualified.Type");

        logger.LogInformation("Processing of {@IncomingRequest} started.", new
        {
            RequestUrl = "https://www.example.com",
            UserAgent = "Steeltoe"
        });

        string logOutput = _consoleOutput.ToString();

        logOutput.Should().Be("""
            [INF] Fully.Qualified.Type: Processing of {"RequestUrl": "https://www.example.com", "UserAgent": "Steeltoe"} started.

            """);
    }

    [Fact]
    public void CallsIntoMessageProcessors()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Serilog:WriteTo:0:Name"] = "Console",
            ["Serilog:WriteTo:0:Args:OutputTemplate"] = "[{Level:u3}] {Properties}{NewLine}  {Message:lj}{NewLine}"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging(loggingBuilder => loggingBuilder.AddDynamicSerilog());
        services.AddSingleton<IDynamicMessageProcessor>(new TestMessageProcessor("One"));
        services.AddSingleton<IDynamicMessageProcessor>(new TestMessageProcessor("Two"));
        using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        IDynamicLoggerProvider provider = serviceProvider.GetServices<ILoggerProvider>().OfType<IDynamicLoggerProvider>().Single();
        ILogger logger = provider.CreateLogger("Test");

        logger.LogInformation("Three");
        string logOutput = _consoleOutput.ToString();

        logOutput.Should().Be("""
            [INF] {SourceContext="Test", Scope=["TwoOne"]}
              Three

            """);
    }

    private static IDynamicLoggerProvider CreateLoggerProvider(Action<ConfigurationBuilder>? configure = null)
    {
        var configurationBuilder = new ConfigurationBuilder();
        configure?.Invoke(configurationBuilder);
        IConfiguration configuration = configurationBuilder.Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddDynamicSerilog();
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        return serviceProvider.GetServices<ILoggerProvider>().OfType<IDynamicLoggerProvider>().Single();
    }

    public void Dispose()
    {
        _consoleOutput.Dispose();
    }

    public enum ConfigurationCategory
    {
        Parent,
        Self,
        Child
    }

    private sealed class LoggerWithDynamicState
    {
        private readonly ConsoleOutput _consoleOutput;
        private readonly ILogger _logger;
        private DynamicLoggerState _dynamicState;

        public string CategoryName { get; }

        public LoggerWithDynamicState(IDynamicLoggerProvider provider, ConsoleOutput consoleOutput, string categoryName)
        {
            _consoleOutput = consoleOutput;
            _logger = provider.CreateLogger(categoryName);
            CategoryName = categoryName;

            ICollection<DynamicLoggerState> dynamicStates = provider.GetLogLevels();
            _dynamicState = FilterDynamicState(dynamicStates);
        }

        public void Refresh(ICollection<DynamicLoggerState> dynamicStates)
        {
            _dynamicState = FilterDynamicState(dynamicStates);
        }

        private DynamicLoggerState FilterDynamicState(ICollection<DynamicLoggerState> dynamicStates)
        {
            return dynamicStates.Single(state => state.CategoryName == CategoryName);
        }

        public void AssertMinLevel(LogLevel effectiveMinLevel, LogLevel? backupLogLevel = null)
        {
            _logger.ProbeMinLevel().Should().Be(effectiveMinLevel);

            string categoryNameInDynamicState = CategoryName.Length == 0 ? "Default" : CategoryName;

            _dynamicState.ToString().Should().Be(backupLogLevel == null
                ? $"{categoryNameInDynamicState}: {effectiveMinLevel}"
                : $"{categoryNameInDynamicState}: {backupLogLevel} -> {effectiveMinLevel}");

            string logOutput = CaptureLogOutput();

            switch (effectiveMinLevel)
            {
                case LogLevel.None:
                {
                    logOutput.Should().BeEmpty();
                    break;
                }
                case LogLevel.Critical:
                {
                    logOutput.Should().Contain($"Test:{CategoryName}:Critical");
                    logOutput.Should().NotContain($"Test:{CategoryName}:Error");
                    logOutput.Should().NotContain($"Test:{CategoryName}:Warning");
                    logOutput.Should().NotContain($"Test:{CategoryName}:Informational");
                    logOutput.Should().NotContain($"Test:{CategoryName}:Debug");
                    logOutput.Should().NotContain($"Test:{CategoryName}:Trace");
                    break;
                }
                case LogLevel.Error:
                {
                    logOutput.Should().Contain($"Test:{CategoryName}:Critical");
                    logOutput.Should().Contain($"Test:{CategoryName}:Error");
                    logOutput.Should().NotContain($"Test:{CategoryName}:Warning");
                    logOutput.Should().NotContain($"Test:{CategoryName}:Informational");
                    logOutput.Should().NotContain($"Test:{CategoryName}:Debug");
                    logOutput.Should().NotContain($"Test:{CategoryName}:Trace");
                    break;
                }
                case LogLevel.Warning:
                {
                    logOutput.Should().Contain($"Test:{CategoryName}:Critical");
                    logOutput.Should().Contain($"Test:{CategoryName}:Error");
                    logOutput.Should().Contain($"Test:{CategoryName}:Warning");
                    logOutput.Should().NotContain($"Test:{CategoryName}:Informational");
                    logOutput.Should().NotContain($"Test:{CategoryName}:Debug");
                    logOutput.Should().NotContain($"Test:{CategoryName}:Trace");
                    break;
                }
                case LogLevel.Information:
                {
                    logOutput.Should().Contain($"Test:{CategoryName}:Critical");
                    logOutput.Should().Contain($"Test:{CategoryName}:Error");
                    logOutput.Should().Contain($"Test:{CategoryName}:Warning");
                    logOutput.Should().Contain($"Test:{CategoryName}:Informational");
                    logOutput.Should().NotContain($"Test:{CategoryName}:Debug");
                    logOutput.Should().NotContain($"Test:{CategoryName}:Trace");
                    break;
                }
                case LogLevel.Debug:
                {
                    logOutput.Should().Contain($"Test:{CategoryName}:Critical");
                    logOutput.Should().Contain($"Test:{CategoryName}:Error");
                    logOutput.Should().Contain($"Test:{CategoryName}:Warning");
                    logOutput.Should().Contain($"Test:{CategoryName}:Informational");
                    logOutput.Should().Contain($"Test:{CategoryName}:Debug");
                    logOutput.Should().NotContain($"Test:{CategoryName}:Trace");
                    break;
                }
                case LogLevel.Trace:
                {
                    logOutput.Should().Contain($"Test:{CategoryName}:Critical");
                    logOutput.Should().Contain($"Test:{CategoryName}:Error");
                    logOutput.Should().Contain($"Test:{CategoryName}:Warning");
                    logOutput.Should().Contain($"Test:{CategoryName}:Informational");
                    logOutput.Should().Contain($"Test:{CategoryName}:Debug");
                    logOutput.Should().Contain($"Test:{CategoryName}:Trace");
                    break;
                }
            }
        }

        private string CaptureLogOutput()
        {
            _consoleOutput.Clear();

#pragma warning disable CA2254 // Template should be a static expression
            _logger.LogCritical($"Test:{CategoryName}:Critical");
            _logger.LogError($"Test:{CategoryName}:Error");
            _logger.LogWarning($"Test:{CategoryName}:Warning");
            _logger.LogInformation($"Test:{CategoryName}:Informational");
            _logger.LogDebug($"Test:{CategoryName}:Debug");
            _logger.LogTrace($"Test:{CategoryName}:Trace");
#pragma warning restore CA2254 // Template should be a static expression

            return _consoleOutput.ToString();
        }
    }

    private sealed class DynamicLoggingTestContext(IDynamicLoggerProvider provider, ConsoleOutput consoleOutput)
    {
        private const string DefaultCategoryName = "";
        private const string ParentCategoryName = "A";
        private const string SelfCategoryName = "A.B";
        private const string ChildCategoryName = "A.B.C";

        private readonly IDynamicLoggerProvider _provider = provider;

        public LoggerWithDynamicState Default { get; } = new(provider, consoleOutput, DefaultCategoryName);
        public LoggerWithDynamicState Parent { get; } = new(provider, consoleOutput, ParentCategoryName);
        public LoggerWithDynamicState Self { get; } = new(provider, consoleOutput, SelfCategoryName);
        public LoggerWithDynamicState Child { get; } = new(provider, consoleOutput, ChildCategoryName);

        public void Refresh()
        {
            ICollection<DynamicLoggerState> dynamicStates = _provider.GetLogLevels();

            Default.Refresh(dynamicStates);
            Parent.Refresh(dynamicStates);
            Self.Refresh(dynamicStates);
            Child.Refresh(dynamicStates);
        }

        public static string ToSerilogLevel(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => "Verbose",
                LogLevel.Critical => "Fatal",
                _ => logLevel.ToString()
            };
        }
    }

    private sealed class TestMessageProcessor(string text) : IDynamicMessageProcessor
    {
        private readonly string _text = text;

        public string Process(string message)
        {
            return _text + message;
        }
    }
}
