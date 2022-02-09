// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Xunit;

namespace Steeltoe.Extensions.Logging.DynamicSerilog.Test
{
    public class SerilogWebApplicationBuilderTest
    {
        public static ILogEventSink[] GetSinks(object logger)
        {
            var loggerField = logger.GetType().GetField("_logger", BindingFlags.NonPublic | BindingFlags.Instance);
            var logger2 = loggerField.GetValue(logger);
            var loggersField = logger2.GetType().GetProperty("Loggers");
            var loggersvalueArray = loggersField.GetValue(logger2) as System.Array;

            var loggersvaluearrayItem = loggersvalueArray.GetValue(0);
            var dynamicLoggerField = loggersvaluearrayItem.GetType().GetProperty("Logger");
            var dynamiclogger = dynamicLoggerField.GetValue(loggersvaluearrayItem) as MessageProcessingLogger;

            var logger3field = dynamiclogger.Delegate.GetType().GetField("_logger", BindingFlags.NonPublic | BindingFlags.Instance);
            var serilogger = logger3field.GetValue(dynamiclogger.Delegate);

            var loggerSinksField = serilogger.GetType().GetField("_sink", BindingFlags.NonPublic | BindingFlags.Instance);
            var serilogger2 = loggerSinksField.GetValue(serilogger);

            var serilogger2SinksField = serilogger2.GetType().GetField("_sink", BindingFlags.NonPublic | BindingFlags.Instance);
            var serilogger3 = serilogger2SinksField.GetValue(serilogger2);

            var serilogger3SinksField = serilogger3.GetType().GetField("_sink", BindingFlags.NonPublic | BindingFlags.Instance);
            var aggregatedSinks = serilogger3SinksField.GetValue(serilogger3);

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
                .AddDynamicSerilog((context, loggerConfiguration) =>
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
            var appsettings = new Dictionary<string, string>()
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

            var logger = host.Services.GetService(typeof(ILogger<SerilogDynamicWebhostBuilderTest>));
            var startup = new Startup(host.Services.GetRequiredService<ILogger<Startup>>(), host.Configuration);
            startup.ConfigureServices(null);
            var sinks = SerilogDynamicWebhostBuilderTest.GetSinks(logger);
            Assert.NotNull(sinks);
            var testSink = sinks.Where(x => x.GetType() == typeof(TestSink)).FirstOrDefault() as TestSink;

            var logs = testSink.GetLogs();
            Assert.NotEmpty(logs);
            Assert.Contains("error", logs);
            Assert.DoesNotContain("info", logs);
        }
    }
}
#endif