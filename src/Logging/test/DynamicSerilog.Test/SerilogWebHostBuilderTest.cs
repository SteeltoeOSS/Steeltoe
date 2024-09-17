// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Logging.DynamicSerilog.Test;

public sealed class SerilogWebHostBuilderTest
{
    public SerilogWebHostBuilderTest()
    {
        DynamicSerilogLoggerProvider.ClearLogger();
    }

    [Fact]
    public void OnlyApplicableFilters_AreApplied()
    {
        var testSink = new TestSink();

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();

        builder.AddDynamicSerilog((_, loggerConfiguration) => loggerConfiguration.MinimumLevel.Error().Enrich.WithExceptionDetails().MinimumLevel
            .Override("Microsoft", LogEventLevel.Warning).WriteTo.Sink(testSink));

        using IWebHost host = builder.Build();

        List<string> logs = testSink.GetLogs();

        logs.Should().NotBeEmpty();
        logs.Should().Contain("error");
        logs.Should().NotContain("info");
    }

    [Fact]
    public void OnlyApplicableFilters_AreApplied_via_Options()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "Serilog:Using:0", "Steeltoe.Logging.DynamicSerilog.Test" },
            { "Serilog:MinimumLevel:Default", "Error" },
            { "Serilog:MinimumLevel:Override:Microsoft", "Warning" },
            { "Serilog:WriteTo:Name", "TestSink" }
        };

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
        builder.UseStartup<Startup>();
        builder.AddDynamicSerilog();

        using IWebHost host = builder.Build();

        object logger = host.Services.GetRequiredService<ILogger<SerilogWebHostBuilderTest>>();
        ILogEventSink[] sinks = GetSinks(logger);
        sinks.Should().NotBeEmpty();

        TestSink? testSink = sinks.OfType<TestSink>().FirstOrDefault();
        testSink.Should().NotBeNull();

        List<string> logs = testSink!.GetLogs();

        logs.Should().NotBeEmpty();
        logs.Should().Contain("error");
        logs.Should().NotContain("info");
    }

    internal static ILogEventSink[] GetSinks(object logger)
    {
        FieldInfo loggerField = logger.GetType().GetField("_logger", BindingFlags.NonPublic | BindingFlags.Instance)!;
        object logger2 = loggerField.GetValue(logger)!;
        PropertyInfo loggersField = logger2.GetType().GetProperty("Loggers")!;
        var loggersValueArray = (Array)loggersField.GetValue(logger2)!;

        object loggersValueArrayItem = loggersValueArray.GetValue(0)!;
        PropertyInfo dynamicLoggerField = loggersValueArrayItem.GetType().GetProperty("Logger")!;
        var dynamicLogger = (MessageProcessingLogger)dynamicLoggerField.GetValue(loggersValueArrayItem)!;

        FieldInfo logger3Field = dynamicLogger.InnerLogger.GetType().GetField("_logger", BindingFlags.NonPublic | BindingFlags.Instance)!;
        object serilogLogger = logger3Field.GetValue(dynamicLogger.InnerLogger)!;

        FieldInfo loggerSinksField = serilogLogger.GetType().GetField("_sink", BindingFlags.NonPublic | BindingFlags.Instance)!;
        object serilogLogger2 = loggerSinksField.GetValue(serilogLogger)!;

        FieldInfo serilogLogger2SinksField = serilogLogger2.GetType().GetField("_sink", BindingFlags.NonPublic | BindingFlags.Instance)!;
        object serilogLogger3 = serilogLogger2SinksField.GetValue(serilogLogger2)!;

        FieldInfo serilogLogger3SinksField = serilogLogger3.GetType().GetField("_sink", BindingFlags.NonPublic | BindingFlags.Instance)!;
        object aggregatedSinks = serilogLogger3SinksField.GetValue(serilogLogger3)!;

        FieldInfo aggregateSinksField = aggregatedSinks.GetType().GetField("_sinks", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var sinks = (ILogEventSink[])aggregateSinksField.GetValue(aggregatedSinks)!;
        return sinks;
    }
}
