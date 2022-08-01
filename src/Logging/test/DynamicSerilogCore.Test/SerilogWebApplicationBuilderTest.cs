// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;
using System.Reflection;
using Xunit;

namespace Steeltoe.Extensions.Logging.DynamicSerilog.Test;

public class SerilogWebApplicationBuilderTest
{
    public static ILogEventSink[] GetSinks(object logger)
    {
        var loggerField = logger.GetType().GetField("_logger", BindingFlags.NonPublic | BindingFlags.Instance);
        var logger2 = loggerField.GetValue(logger);
        var loggersField = logger2.GetType().GetProperty("Loggers");
        var loggersValueArray = loggersField.GetValue(logger2) as Array;

        var loggersValueArrayItem = loggersValueArray.GetValue(0);
        var dynamicLoggerField = loggersValueArrayItem.GetType().GetProperty("Logger");
        var dynamicLogger = dynamicLoggerField.GetValue(loggersValueArrayItem) as MessageProcessingLogger;

        var logger3Field = dynamicLogger.Delegate.GetType().GetField("_logger", BindingFlags.NonPublic | BindingFlags.Instance);
        var serilogLogger = logger3Field.GetValue(dynamicLogger.Delegate);

        var loggerSinksField = serilogLogger.GetType().GetField("_sink", BindingFlags.NonPublic | BindingFlags.Instance);
        var serilogLogger2 = loggerSinksField.GetValue(serilogLogger);

        var serilogLogger2SinksField = serilogLogger2.GetType().GetField("_sink", BindingFlags.NonPublic | BindingFlags.Instance);
        var serilogLogger3 = serilogLogger2SinksField.GetValue(serilogLogger2);

        var serilogLogger3SinksField = serilogLogger3.GetType().GetField("_sink", BindingFlags.NonPublic | BindingFlags.Instance);
        var aggregatedSinks = serilogLogger3SinksField.GetValue(serilogLogger3);

        var aggregateSinksField = aggregatedSinks.GetType().GetField("_sinks", BindingFlags.NonPublic | BindingFlags.Instance);
        var sinks = (ILogEventSink[])aggregateSinksField.GetValue(aggregatedSinks);
        return sinks;
    }

    public SerilogWebApplicationBuilderTest()
    {
        SerilogDynamicProvider.ClearLogger();
    }

    [Fact]
    public void OnlyApplicableFilters_AreApplied()
    {
        var testSink = new TestSink();

        var host = TestHelpers.GetTestWebApplicationBuilder()
            .AddDynamicSerilog((_, loggerConfiguration) =>
            {
                loggerConfiguration
                    .MinimumLevel.Error()
                    .Enrich.WithExceptionDetails()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .WriteTo.Sink(testSink);
            })
            .Build();
        var startup = new Startup(host.Services.GetRequiredService<ILogger<Startup>>(), host.Configuration);
        startup.ConfigureServices(null);

        var logs = testSink.GetLogs();
        Assert.NotEmpty(logs);
        Assert.Contains("error", logs);
        Assert.DoesNotContain("info", logs);
    }

    [Fact]
    public void OnlyApplicableFilters_AreApplied_via_Options()
    {
        var appsettings = new Dictionary<string, string>
        {
            { "Serilog:Using:0", "Steeltoe.Extensions.Logging.DynamicSerilogCore.Test" },
            { "Serilog:MinimumLevel:Default", "Error" },
            { "Serilog:MinimumLevel:Override:Microsoft", "Warning" },
            { "Serilog:WriteTo:Name", "TestSink" }
        };

        var hostBuilder = TestHelpers.GetTestWebApplicationBuilder();
        hostBuilder.Configuration.AddInMemoryCollection(appsettings);
        hostBuilder.AddDynamicSerilog();
        var host = hostBuilder.Build();

        var logger = host.Services.GetService(typeof(ILogger<SerilogDynamicWebHostBuilderTest>));
        var startup = new Startup(host.Services.GetRequiredService<ILogger<Startup>>(), host.Configuration);
        startup.ConfigureServices(null);
        var sinks = SerilogDynamicWebHostBuilderTest.GetSinks(logger);
        Assert.NotNull(sinks);
        var testSink = sinks.FirstOrDefault(x => x.GetType() == typeof(TestSink)) as TestSink;

        var logs = testSink.GetLogs();
        Assert.NotEmpty(logs);
        Assert.Contains("error", logs);
        Assert.DoesNotContain("info", logs);
    }
}

