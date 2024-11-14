// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Logging.DynamicLogger.Test;

public sealed class DynamicConsoleLoggerProviderTest : IDisposable
{
    private readonly ConsoleOutput _consoleOutput = ConsoleOutput.Capture();

    [Fact]
    public async Task ConsoleSettingsWinOverGlobalSettings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Default"] = "Error",
            ["Logging:LogLevel:A.B"] = "Warning",
            ["Logging:Console:LogLevel:Default"] = "Trace",
            ["Logging:Console:LogLevel:A.B.C"] = "Debug"
        };

        using IDynamicLoggerProvider provider = CreateLoggerProvider(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
        DynamicLoggingTestContext testContext = new(provider, _consoleOutput);

        await testContext.Parent.AssertMinLevelAsync(LogLevel.Trace);
        await testContext.Self.AssertMinLevelAsync(LogLevel.Warning);
        await testContext.Child.AssertMinLevelAsync(LogLevel.Debug);
    }

    [Fact]
    public void FailsOnWildcardInConfiguration()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Some*Other"] = "Information"
        };

        Action action = () => CreateLoggerProvider(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));

        action.Should().ThrowExactly<NotSupportedException>().WithMessage("Logger categories with wildcards are not supported.");
    }

    [Fact]
    public void CreatesLoggerWithCorrectFilters()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Fully.Qualified"] = "Warning"
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
    public void CreateLoggerMultipleTimesReturnsSameLoggerInstance()
    {
        using IDynamicLoggerProvider provider = CreateLoggerProvider();

        ILogger firstLogger = provider.CreateLogger("Some");
        ILogger nextLogger = provider.CreateLogger("Some");

        firstLogger.Should().BeSameAs(nextLogger);
    }

    [Fact]
    public void GetLogLevelsReturnsIntermediateCategories()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Logging:LogLevel:A"] = "Trace"
        };

        using IDynamicLoggerProvider provider = CreateLoggerProvider(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
        _ = provider.CreateLogger("A.B.C.D.Example");

        string[] loggerStates = provider.GetLogLevels().Select(state => state.ToString()).ToArray();

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
    public async Task CanSetAndResetMinLevel(ConfigurationCategory configurationCategory, LogLevel configurationLevel, LogLevel overrideLevel)
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
            [$"Logging:LogLevel:{configurationKey}"] = configurationLevel.ToString()
        };

        LogLevel expectBeforeLevelAtParent = configurationCategory == ConfigurationCategory.Parent ? configurationLevel : LogLevel.Information;
        LogLevel expectBeforeLevelAtSelf = configurationCategory != ConfigurationCategory.Child ? configurationLevel : LogLevel.Information;
        LogLevel expectBeforeLevelAtChild = configurationLevel;

        using IDynamicLoggerProvider provider = CreateLoggerProvider(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
        DynamicLoggingTestContext testContext = new(provider, _consoleOutput);

        await testContext.Parent.AssertMinLevelAsync(expectBeforeLevelAtParent);
        await testContext.Self.AssertMinLevelAsync(expectBeforeLevelAtSelf);
        await testContext.Child.AssertMinLevelAsync(expectBeforeLevelAtChild);

        provider.SetLogLevel(testContext.Self.CategoryName, overrideLevel);
        testContext.Refresh();

        await testContext.Parent.AssertMinLevelAsync(expectBeforeLevelAtParent);
        await testContext.Self.AssertMinLevelAsync(overrideLevel, expectBeforeLevelAtSelf);
        await testContext.Child.AssertMinLevelAsync(overrideLevel);

        provider.SetLogLevel(testContext.Self.CategoryName, null);
        testContext.Refresh();

        await testContext.Parent.AssertMinLevelAsync(expectBeforeLevelAtParent);
        await testContext.Self.AssertMinLevelAsync(expectBeforeLevelAtSelf);
        await testContext.Child.AssertMinLevelAsync(expectBeforeLevelAtChild);
    }

    [Fact]
    public async Task CanSetAndResetImplicitDefaultMinLevel()
    {
        using IDynamicLoggerProvider provider = CreateLoggerProvider();
        DynamicLoggingTestContext testContext = new(provider, _consoleOutput);

        await testContext.Default.AssertMinLevelAsync(LogLevel.Information);
        await testContext.Self.AssertMinLevelAsync(LogLevel.Information);

        provider.SetLogLevel(string.Empty, LogLevel.Error);
        testContext.Refresh();

        await testContext.Default.AssertMinLevelAsync(LogLevel.Error, LogLevel.Information);
        await testContext.Self.AssertMinLevelAsync(LogLevel.Error);

        provider.SetLogLevel(string.Empty, null);
        testContext.Refresh();

        await testContext.Default.AssertMinLevelAsync(LogLevel.Information);
        await testContext.Self.AssertMinLevelAsync(LogLevel.Information);
    }

    [Fact]
    public async Task ResetClearsOverridesInDescendants()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Logging:LogLevel:A.B.C"] = "Error"
        };

        using IDynamicLoggerProvider provider = CreateLoggerProvider(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
        DynamicLoggingTestContext testContext = new(provider, _consoleOutput);

        provider.SetLogLevel(testContext.Self.CategoryName, LogLevel.Trace);
        testContext.Refresh();

        await testContext.Parent.AssertMinLevelAsync(LogLevel.Information);
        await testContext.Self.AssertMinLevelAsync(LogLevel.Trace, LogLevel.Information);
        await testContext.Child.AssertMinLevelAsync(LogLevel.Trace);

        provider.SetLogLevel(testContext.Parent.CategoryName, LogLevel.Debug);
        testContext.Refresh();

        await testContext.Parent.AssertMinLevelAsync(LogLevel.Debug, LogLevel.Information);
        await testContext.Self.AssertMinLevelAsync(LogLevel.Debug);
        await testContext.Child.AssertMinLevelAsync(LogLevel.Debug);

        provider.SetLogLevel(testContext.Parent.CategoryName, null);
        testContext.Refresh();

        await testContext.Parent.AssertMinLevelAsync(LogLevel.Information);
        await testContext.Self.AssertMinLevelAsync(LogLevel.Information);
        await testContext.Child.AssertMinLevelAsync(LogLevel.Error);
    }

    [Fact]
    public async Task ResetAtDescendantPreservesOverrideInParent()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Logging:LogLevel:A.B"] = "Critical",
            ["Logging:LogLevel:A.B.C"] = "Error"
        };

        using IDynamicLoggerProvider provider = CreateLoggerProvider(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
        DynamicLoggingTestContext testContext = new(provider, _consoleOutput);

        provider.SetLogLevel(testContext.Parent.CategoryName, LogLevel.Trace);
        provider.SetLogLevel(testContext.Self.CategoryName, LogLevel.Debug);
        provider.SetLogLevel(testContext.Child.CategoryName, LogLevel.Warning);
        testContext.Refresh();

        await testContext.Parent.AssertMinLevelAsync(LogLevel.Trace, LogLevel.Information);
        await testContext.Self.AssertMinLevelAsync(LogLevel.Debug, LogLevel.Critical);
        await testContext.Child.AssertMinLevelAsync(LogLevel.Warning, LogLevel.Error);

        provider.SetLogLevel(testContext.Child.CategoryName, null);
        testContext.Refresh();

        await testContext.Parent.AssertMinLevelAsync(LogLevel.Trace, LogLevel.Information);
        await testContext.Self.AssertMinLevelAsync(LogLevel.Debug, LogLevel.Critical);
        await testContext.Child.AssertMinLevelAsync(LogLevel.Debug);
    }

    [Fact]
    public async Task CanResetForMissingOverride()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Logging:LogLevel:A.B.C"] = "Error"
        };

        using IDynamicLoggerProvider provider = CreateLoggerProvider(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
        DynamicLoggingTestContext testContext = new(provider, _consoleOutput);

        provider.SetLogLevel(testContext.Self.CategoryName, LogLevel.Trace);
        testContext.Refresh();

        await testContext.Parent.AssertMinLevelAsync(LogLevel.Information);
        await testContext.Self.AssertMinLevelAsync(LogLevel.Trace, LogLevel.Information);
        await testContext.Child.AssertMinLevelAsync(LogLevel.Trace);

        provider.SetLogLevel(testContext.Parent.CategoryName, null);
        testContext.Refresh();

        await testContext.Parent.AssertMinLevelAsync(LogLevel.Information);
        await testContext.Self.AssertMinLevelAsync(LogLevel.Information);
        await testContext.Child.AssertMinLevelAsync(LogLevel.Error);
    }

    [Fact]
    public async Task CanSetOrResetMultipleTimes()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Logging:LogLevel:A"] = "Error"
        };

        using IDynamicLoggerProvider provider = CreateLoggerProvider(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
        DynamicLoggingTestContext testContext = new(provider, _consoleOutput);

        await testContext.Parent.AssertMinLevelAsync(LogLevel.Error);
        await testContext.Self.AssertMinLevelAsync(LogLevel.Error);
        await testContext.Child.AssertMinLevelAsync(LogLevel.Error);

        provider.SetLogLevel(testContext.Self.CategoryName, LogLevel.None);
        testContext.Refresh();

        await testContext.Parent.AssertMinLevelAsync(LogLevel.Error);
        await testContext.Self.AssertMinLevelAsync(LogLevel.None, LogLevel.Error);
        await testContext.Child.AssertMinLevelAsync(LogLevel.None);

        provider.SetLogLevel(testContext.Self.CategoryName, LogLevel.Debug);
        testContext.Refresh();

        await testContext.Parent.AssertMinLevelAsync(LogLevel.Error);
        await testContext.Self.AssertMinLevelAsync(LogLevel.Debug, LogLevel.Error);
        await testContext.Child.AssertMinLevelAsync(LogLevel.Debug);

        provider.SetLogLevel(testContext.Parent.CategoryName, null);
        testContext.Refresh();

        await testContext.Parent.AssertMinLevelAsync(LogLevel.Error);
        await testContext.Self.AssertMinLevelAsync(LogLevel.Error);
        await testContext.Child.AssertMinLevelAsync(LogLevel.Error);

        provider.SetLogLevel(testContext.Parent.CategoryName, null);
        testContext.Refresh();

        await testContext.Parent.AssertMinLevelAsync(LogLevel.Error);
        await testContext.Self.AssertMinLevelAsync(LogLevel.Error);
        await testContext.Child.AssertMinLevelAsync(LogLevel.Error);
    }

    [Fact]
    public void SetIsAppliedToBothExistingAndNewLogger()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Some"] = "Trace"
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
    public async Task ProperlySplitsCategories()
    {
        const string someCategoryName = "Some";
        const string someWithSuffixCategoryName = "SomeWithSuffix";

        var appSettings = new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Default"] = "Warning",
            [$"Logging:LogLevel:{someCategoryName}"] = "Critical"
        };

        using IDynamicLoggerProvider provider = CreateLoggerProvider(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
        LoggerWithDynamicState someState = new(provider, _consoleOutput, someCategoryName);
        LoggerWithDynamicState someWithSuffixState = new(provider, _consoleOutput, someWithSuffixCategoryName);

        await someState.AssertMinLevelAsync(LogLevel.Critical);
        await someWithSuffixState.AssertMinLevelAsync(LogLevel.Warning);

        provider.SetLogLevel(someCategoryName, LogLevel.Debug);
        Refresh();

        await someState.AssertMinLevelAsync(LogLevel.Debug, LogLevel.Critical);
        await someWithSuffixState.AssertMinLevelAsync(LogLevel.Warning);

        provider.SetLogLevel(someCategoryName, null);
        Refresh();

        await someState.AssertMinLevelAsync(LogLevel.Critical);
        await someWithSuffixState.AssertMinLevelAsync(LogLevel.Warning);

        void Refresh()
        {
            ICollection<DynamicLoggerState> logLevels = provider.GetLogLevels();

            someState.Refresh(logLevels);
            someWithSuffixState.Refresh(logLevels);
        }
    }

    [Fact]
    public async Task CategoriesAreCaseSensitive()
    {
        const string pascalCaseCategoryName = "Some";
        const string upperCaseCategoryName = "SOME";

        var appSettings = new Dictionary<string, string?>
        {
            [$"Logging:LogLevel:{pascalCaseCategoryName}"] = "Critical"
        };

        using IDynamicLoggerProvider provider = CreateLoggerProvider(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
        LoggerWithDynamicState pascalCaseState = new(provider, _consoleOutput, pascalCaseCategoryName);
        LoggerWithDynamicState upperCaseState = new(provider, _consoleOutput, upperCaseCategoryName);

        provider.SetLogLevel(pascalCaseCategoryName, LogLevel.Trace);
        Refresh();

        await pascalCaseState.AssertMinLevelAsync(LogLevel.Trace, LogLevel.Critical);
        await upperCaseState.AssertMinLevelAsync(LogLevel.Information);

        void Refresh()
        {
            ICollection<DynamicLoggerState> logLevels = provider.GetLogLevels();

            pascalCaseState.Refresh(logLevels);
            upperCaseState.Refresh(logLevels);
        }
    }

    [Fact]
    public async Task AppliesChangedConfiguration()
    {
        const string fileName = "appsettings.json";
        MemoryFileProvider fileProvider = new();

        fileProvider.IncludeFile(fileName, """
        {
          "Logging": {
            "LogLevel": {
              "A": "Warning"
            }
          }
        }
        """);

        using IDynamicLoggerProvider provider =
            CreateLoggerProvider(configurationBuilder => configurationBuilder.AddJsonFile(fileProvider, fileName, false, true));

        DynamicLoggingTestContext testContext = new(provider, _consoleOutput);

        await testContext.Parent.AssertMinLevelAsync(LogLevel.Warning);
        await testContext.Self.AssertMinLevelAsync(LogLevel.Warning);
        await testContext.Child.AssertMinLevelAsync(LogLevel.Warning);

        provider.SetLogLevel(testContext.Self.CategoryName, LogLevel.Error);
        testContext.Refresh();

        await testContext.Parent.AssertMinLevelAsync(LogLevel.Warning);
        await testContext.Self.AssertMinLevelAsync(LogLevel.Error, LogLevel.Warning);
        await testContext.Child.AssertMinLevelAsync(LogLevel.Error);

        fileProvider.ReplaceFile(fileName, """
        {
          "Logging": {
            "LogLevel": {
              "A": "Trace",
              "A.B.C": "Debug"
            }
          }
        }
        """);

        fileProvider.NotifyChanged();
        testContext.Refresh();

        await testContext.Parent.AssertMinLevelAsync(LogLevel.Trace);
        await testContext.Self.AssertMinLevelAsync(LogLevel.Error, LogLevel.Trace);
        await testContext.Child.AssertMinLevelAsync(LogLevel.Error);

        provider.SetLogLevel(testContext.Self.CategoryName, null);
        testContext.Refresh();

        await testContext.Parent.AssertMinLevelAsync(LogLevel.Trace);
        await testContext.Self.AssertMinLevelAsync(LogLevel.Trace);
        await testContext.Child.AssertMinLevelAsync(LogLevel.Debug);
    }

    [Fact]
    public async Task CanUseJsonFormatterWithScopes()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Logging:Console:FormatterName"] = "json",
            ["Logging:Console:FormatterOptions:IncludeScopes"] = "true",
            ["Logging:Console:FormatterOptions:TimestampFormat"] = string.Empty,
            ["Logging:Console:FormatterOptions:JsonWriterOptions:Indented"] = "true"
        };

        using IDynamicLoggerProvider provider = CreateLoggerProvider(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));

        using var factory = new LoggerFactory();
        factory.AddProvider(provider);
        ILogger logger = factory.CreateLogger("Fully.Qualified.Type");

        using (logger.BeginScope("OuterScope"))
        {
            using (logger.BeginScope("InnerScope={InnerScopeKey}", "InnerScopeValue"))
            {
                logger.LogInformation("Processing of {@IncomingRequest} started.", new
                {
                    RequestUrl = "https://www.example.com",
                    UserAgent = "Steeltoe"
                });
            }
        }

        await _consoleOutput.WaitForFlushAsync();
        string logOutput = _consoleOutput.ToString();

        logOutput.Should().Be("""
            {
              "EventId": 0,
              "LogLevel": "Information",
              "Category": "Fully.Qualified.Type",
              "Message": "Processing of { RequestUrl = https://www.example.com, UserAgent = Steeltoe } started.",
              "State": {
                "Message": "Processing of { RequestUrl = https://www.example.com, UserAgent = Steeltoe } started.",
                "@IncomingRequest": "{ RequestUrl = https://www.example.com, UserAgent = Steeltoe }",
                "{OriginalFormat}": "Processing of {@IncomingRequest} started."
              },
              "Scopes": [
                "OuterScope",
                {
                  "Message": "InnerScope=InnerScopeValue",
                  "InnerScopeKey": "InnerScopeValue",
                  "{OriginalFormat}": "InnerScope={InnerScopeKey}"
                }
              ]
            }

            """);
    }

    [Fact]
    public async Task CallsIntoMessageProcessors()
    {
        var services = new ServiceCollection();
        services.AddLogging(loggingBuilder => loggingBuilder.AddDynamicConsole());
        services.AddSingleton<IDynamicMessageProcessor>(new TestMessageProcessor("One"));
        services.AddSingleton<IDynamicMessageProcessor>(new TestMessageProcessor("Two"));
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        IDynamicLoggerProvider provider = serviceProvider.GetServices<ILoggerProvider>().OfType<IDynamicLoggerProvider>().Single();
        ILogger logger = provider.CreateLogger("Test");

        logger.LogInformation("Three");

        await _consoleOutput.WaitForFlushAsync();
        string logOutput = _consoleOutput.ToString();

        logOutput.Should().Contain("One");
        logOutput.Should().Contain("Two");
        logOutput.Should().Contain("Three");
    }

    private static IDynamicLoggerProvider CreateLoggerProvider(Action<ConfigurationBuilder>? configure = null)
    {
        var configurationBuilder = new ConfigurationBuilder();
        configure?.Invoke(configurationBuilder);
        IConfiguration configuration = configurationBuilder.Build();

        var services = new ServiceCollection();

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
            loggingBuilder.AddDynamicConsole();
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

        public async Task AssertMinLevelAsync(LogLevel effectiveMinLevel, LogLevel? backupLogLevel = null)
        {
            _logger.ProbeMinLevel().Should().Be(effectiveMinLevel);

            string categoryNameInDynamicState = CategoryName.Length == 0 ? "Default" : CategoryName;

            _dynamicState.ToString().Should().Be(backupLogLevel == null
                ? $"{categoryNameInDynamicState}: {effectiveMinLevel}"
                : $"{categoryNameInDynamicState}: {backupLogLevel} -> {effectiveMinLevel}");

            string logOutput = await CaptureLogOutputAsync();

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

        private async Task<string> CaptureLogOutputAsync()
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

            await _consoleOutput.WaitForFlushAsync();

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
