// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Common.Logging.Test;

public sealed class BootstrapperLoggerFactoryTest
{
    [Fact]
    public async Task Upgrades_existing_loggers()
    {
        var capturingLoggerProvider = new CapturingLoggerProvider(categoryName => categoryName.StartsWith("Test", StringComparison.Ordinal));

        var bootstrapLoggerFactory = BootstrapLoggerFactory.CreateEmpty(loggingBuilder =>
        {
            loggingBuilder.SetMinimumLevel(LogLevel.Trace);
            loggingBuilder.AddProvider(capturingLoggerProvider);
        });

        ILogger beforeStartLogger = bootstrapLoggerFactory.CreateLogger("Test_BeforeStart");
        beforeStartLogger.LogTrace("Initializing (trace)");

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.Logging.AddProvider(capturingLoggerProvider);
        builder.Services.UpgradeBootstrapLoggerFactory(bootstrapLoggerFactory);

        await using WebApplication app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
        ILogger afterStartLogger = loggerFactory.CreateLogger("Test_AfterStart");

        afterStartLogger.LogTrace("Running (trace, ignored)");
        afterStartLogger.LogWarning("Running (warning)");
        beforeStartLogger.LogTrace("Running (trace, ignored)");
        beforeStartLogger.LogWarning("Running (warning)");

        IList<string> messages = capturingLoggerProvider.GetAll();

        messages.Should().BeEquivalentTo([
            "TRCE Test_BeforeStart: Initializing (trace)",
            "WARN Test_AfterStart: Running (warning)",
            "WARN Test_BeforeStart: Running (warning)"
        ], options => options.WithStrictOrdering());
    }

    [Fact]
    public void Creates_default_minimum_levels()
    {
        var bootstrapLoggerFactory = BootstrapLoggerFactory.CreateConsole();
        ILogger logger = bootstrapLoggerFactory.CreateLogger("TestLogger");

        logger.IsEnabled(LogLevel.Trace).Should().BeFalse();
        logger.IsEnabled(LogLevel.Debug).Should().BeFalse();
        logger.IsEnabled(LogLevel.Information).Should().BeTrue();
        logger.IsEnabled(LogLevel.Warning).Should().BeTrue();
        logger.IsEnabled(LogLevel.Error).Should().BeTrue();
        logger.IsEnabled(LogLevel.Critical).Should().BeTrue();
    }

    [Fact]
    public void Can_override_minimum_level()
    {
        var bootstrapLoggerFactory = BootstrapLoggerFactory.CreateConsole(loggingBuilder => loggingBuilder.AddConfiguration(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LogLevel:TestLogger"] = "Warning"
            }).Build()));

        ILogger logger = bootstrapLoggerFactory.CreateLogger("TestLogger");

        logger.IsEnabled(LogLevel.Trace).Should().BeFalse();
        logger.IsEnabled(LogLevel.Debug).Should().BeFalse();
        logger.IsEnabled(LogLevel.Information).Should().BeFalse();
        logger.IsEnabled(LogLevel.Warning).Should().BeTrue();
        logger.IsEnabled(LogLevel.Error).Should().BeTrue();
        logger.IsEnabled(LogLevel.Critical).Should().BeTrue();
    }
}
