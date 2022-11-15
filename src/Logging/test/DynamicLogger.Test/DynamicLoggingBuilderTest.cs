// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using A.B.C.D;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Xunit;

namespace Steeltoe.Logging.DynamicLogger.Test;

public class DynamicLoggingBuilderTest
{
    private static readonly Dictionary<string, string> Appsettings = new()
    {
        ["Logging:IncludeScopes"] = "false",
        ["Logging:Console:LogLevel:Default"] = "Information",
        ["Logging:Console:LogLevel:A.B.C.D"] = "Critical",
        ["Logging:Console:DisableColors"] = "True",
        ["Logging:LogLevel:Steeltoe.Logging.DynamicLogger.Test"] = "Information",
        ["Logging:LogLevel:Default"] = "Warning"
    };

    [Fact]
    public void OnlyApplicableFilters_AreApplied()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["Logging:IncludeScopes"] = "false",
            ["Logging:LogLevel:Default"] = "Information",
            ["Logging:foo:LogLevel:A.B.C.D.TestClass"] = "None"
        };

        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();

        ServiceProvider services = new ServiceCollection().AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        }).BuildServiceProvider();

        var logger = services.GetService(typeof(ILogger<TestClass>)) as ILogger<TestClass>;

        Assert.NotNull(logger);
        Assert.True(logger.IsEnabled(LogLevel.Information), "Information level should be enabled");
        Assert.False(logger.IsEnabled(LogLevel.Debug), "Debug level should NOT be enabled");
    }

    [Fact]
    public void DynamicLevelSetting_WorksWith_ConsoleFilters()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();

        ServiceProvider services = new ServiceCollection().AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        }).BuildServiceProvider();

        var logger = services.GetService(typeof(ILogger<TestClass>)) as ILogger<TestClass>;

        Assert.NotNull(logger);
        Assert.True(logger.IsEnabled(LogLevel.Critical), "Critical level should be enabled");
        Assert.False(logger.IsEnabled(LogLevel.Error), "Error level should NOT be enabled");
        Assert.False(logger.IsEnabled(LogLevel.Warning), "Warning level should NOT be enabled");
        Assert.False(logger.IsEnabled(LogLevel.Debug), "Debug level should NOT be enabled");
        Assert.False(logger.IsEnabled(LogLevel.Trace), "Trace level should NOT be enabled yet");

        // change the log level and confirm it worked
        var provider = services.GetRequiredService(typeof(ILoggerProvider)) as DynamicConsoleLoggerProvider;
        provider.SetLogLevel("A.B.C.D", LogLevel.Trace);
        Assert.True(logger.IsEnabled(LogLevel.Trace), "Trace level should have been enabled");
    }

    [Fact]
    public void AddConsole_Works_WithAddConfiguration()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();

        ServiceProvider services = new ServiceCollection().AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        }).BuildServiceProvider();

        var logger = services.GetService(typeof(ILogger<DynamicLoggingBuilderTest>)) as ILogger<DynamicLoggingBuilderTest>;

        Assert.NotNull(logger);
        Assert.True(logger.IsEnabled(LogLevel.Warning), "Warning level should be enabled");
        Assert.False(logger.IsEnabled(LogLevel.Debug), "Debug level should NOT be enabled");
    }

    [Fact]
    public void AddDynamicConsole_Works_WithAddConfiguration()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();

        ServiceProvider services = new ServiceCollection().AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        }).BuildServiceProvider();

        var logger = services.GetService(typeof(ILogger<DynamicLoggingBuilderTest>)) as ILogger<DynamicLoggingBuilderTest>;

        Assert.NotNull(logger);
        Assert.True(logger.IsEnabled(LogLevel.Warning), "Warning level should be enabled");
        Assert.False(logger.IsEnabled(LogLevel.Debug), "Debug level should NOT be enabled");
    }

    [Fact]
    public void DynamicLevelSetting_ParameterlessAddDynamic_NotBrokenByAddConfiguration()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();

        ServiceProvider services = new ServiceCollection().AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        }).BuildServiceProvider();

        var logger = services.GetService(typeof(ILogger<DynamicLoggingBuilderTest>)) as ILogger<DynamicLoggingBuilderTest>;

        Assert.NotNull(logger);
        Assert.True(logger.IsEnabled(LogLevel.Warning), "Warning level should be enabled");
        Assert.False(logger.IsEnabled(LogLevel.Debug), "Debug level should NOT be enabled");
        Assert.False(logger.IsEnabled(LogLevel.Trace), "Trace level should not be enabled yet");

        // change the log level and confirm it worked
        var provider = services.GetRequiredService(typeof(ILoggerProvider)) as DynamicConsoleLoggerProvider;
        provider.SetLogLevel("Steeltoe.Logging.DynamicLogger.Test", LogLevel.Trace);
        Assert.True(logger.IsEnabled(LogLevel.Trace), "Trace level should have been enabled");
    }

    [Fact]
    public void AddDynamicConsole_WithIDynamicMessageProcessor_CallsProcessMessage()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();

        ServiceProvider services = new ServiceCollection().AddSingleton<IDynamicMessageProcessor, TestDynamicMessageProcessor>().AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        }).BuildServiceProvider();

        var logger = services.GetService(typeof(ILogger<DynamicLoggingBuilderTest>)) as ILogger<DynamicLoggingBuilderTest>;

        Assert.NotNull(logger);

        logger.LogInformation("This is a test");

        var processor = services.GetService<IDynamicMessageProcessor>() as TestDynamicMessageProcessor;
        Assert.NotNull(processor);
        Assert.True(processor.ProcessCalled);
    }

    [Fact]
    public void AddDynamicConsole_AddsAllLoggerProviders()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();

        ServiceProvider services = new ServiceCollection().AddSingleton<IDynamicMessageProcessor, TestDynamicMessageProcessor>().AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        }).BuildServiceProvider();

        var dynamicLoggerProvider = services.GetService<IDynamicLoggerProvider>();
        IEnumerable<ILoggerProvider> logProviders = services.GetServices<ILoggerProvider>();

        Assert.NotNull(dynamicLoggerProvider);
        Assert.NotEmpty(logProviders);
        Assert.Single(logProviders);
        Assert.IsType<DynamicConsoleLoggerProvider>(logProviders.SingleOrDefault());
    }

    [Fact]
    public void DynamicLevelSetting_ParameterlessAddDynamic_AddsConsoleOptions()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();

        ServiceProvider services = new ServiceCollection().AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        }).BuildServiceProvider();

        var options = services.GetService<IOptionsMonitor<ConsoleLoggerOptions>>();

        Assert.NotNull(options);
        Assert.NotNull(options.CurrentValue);
    }

    [Fact]
    public void AddDynamicConsole_DoesNotSetColorLocal()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()).Build();

        ServiceProvider services = new ServiceCollection().AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        }).BuildServiceProvider();

        var options = services.GetService(typeof(IOptions<ConsoleLoggerOptions>)) as IOptions<ConsoleLoggerOptions>;

        Assert.NotNull(options);
    }

    [Fact]
    public void AddDynamicConsole_DisablesColorOnPivotalPlatform()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", "not empty");
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()).Build();

        ServiceProvider services = new ServiceCollection().AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDynamicConsole();
        }).BuildServiceProvider();

        var options = services.GetService(typeof(IOptions<ConsoleLoggerOptions>)) as IOptions<ConsoleLoggerOptions>;

        Assert.NotNull(options);
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", string.Empty);
    }
}
