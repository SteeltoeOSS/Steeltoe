// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using A.B.C.D;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Xunit;

namespace Steeltoe.Logging.DynamicSerilog.Test;

public class SerilogDynamicLoggingBuilderTest
{
    private static readonly Dictionary<string, string> Appsettings = new()
    {
        { "Serilog:MinimumLevel:Default", "Verbose" },
        { "Serilog:MinimumLevel:Override:Microsoft", "Warning" },
        { "Serilog:MinimumLevel:Override:Steeltoe.Extensions", "Verbose" },
        { "Serilog:MinimumLevel:Override:Steeltoe", "Information" },
        { "Serilog:MinimumLevel:Override:A", "Information" },
        { "Serilog:MinimumLevel:Override:A.B.C.D", "Fatal" },
        { "Serilog:WriteTo:Name", "Console" }
    };

    [Fact]
    public void OnlyApplicableFilters_AreApplied()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["Serilog:MinimumLevel:Default"] = "Information"
        };

        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();

        ServiceProvider services = new ServiceCollection().AddSingleton<IConfiguration>(configuration).AddLogging(builder =>
        {
            builder.AddDynamicSerilog();
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

        ServiceProvider services = new ServiceCollection().AddSingleton<IConfiguration>(configuration).AddLogging(builder =>
        {
            builder.AddDynamicSerilog();
        }).BuildServiceProvider();

        var logger = services.GetService(typeof(ILogger<TestClass>)) as ILogger<TestClass>;

        Assert.NotNull(logger);
        Assert.True(logger.IsEnabled(LogLevel.Critical), "Critical level should be enabled");
        Assert.False(logger.IsEnabled(LogLevel.Error), "Error level should NOT be enabled");
        Assert.False(logger.IsEnabled(LogLevel.Warning), "Warning level should NOT be enabled");
        Assert.False(logger.IsEnabled(LogLevel.Debug), "Debug level should NOT be enabled");
        Assert.False(logger.IsEnabled(LogLevel.Trace), "Trace level should NOT be enabled yet");

        // change the log level and confirm it worked
        var provider = services.GetRequiredService(typeof(ILoggerProvider)) as SerilogDynamicProvider;
        provider.SetLogLevel("A.B.C.D", LogLevel.Trace);

        IEnumerable<LogLevel> levels = provider.GetLoggerConfigurations().Where(c => c.Name.StartsWith("A.B.C.D", StringComparison.Ordinal))
            .Select(x => x.EffectiveLevel);

        Assert.NotNull(levels);
        Assert.True(levels.All(x => x == LogLevel.Trace));
    }

    [Fact]
    public void AddDynamicSerilogPreservesDefaultLoggerWhenTrue()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();
        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration).AddSingleton<ConsoleLoggerProvider>().AddLogging(builder =>
        {
            builder.AddDynamicSerilog(true);
        }).BuildServiceProvider();

        Assert.Contains(services, d => d.ImplementationType == typeof(ConsoleLoggerProvider));
    }

    [Fact]
    public void AddDynamicConsole_AddsAllLoggerProviders()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();

        ServiceProvider services = new ServiceCollection().AddSingleton<IConfiguration>(configuration).AddLogging(builder =>
        {
            builder.AddDynamicSerilog();
        }).BuildServiceProvider();

        var dynamicLoggerProvider = services.GetService<IDynamicLoggerProvider>();
        IEnumerable<ILoggerProvider> logProviders = services.GetServices<ILoggerProvider>();

        Assert.NotNull(dynamicLoggerProvider);
        Assert.NotEmpty(logProviders);
        Assert.Single(logProviders);
        Assert.IsType<SerilogDynamicProvider>(logProviders.SingleOrDefault());
    }

    [Fact]
    public void AddDynamicConsole_WithConfigurationParam_AddsServices()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();

        ServiceProvider services = new ServiceCollection().AddSingleton<IConfiguration>(configuration).AddLogging(builder => builder.AddDynamicSerilog())
            .BuildServiceProvider();

        var dynamicLoggerProvider = services.GetService<IDynamicLoggerProvider>();
        IEnumerable<ILoggerProvider> logProviders = services.GetServices<ILoggerProvider>();

        Assert.NotNull(dynamicLoggerProvider);
        Assert.NotEmpty(logProviders);
        Assert.Single(logProviders);
        Assert.IsType<SerilogDynamicProvider>(logProviders.SingleOrDefault());
    }
}
