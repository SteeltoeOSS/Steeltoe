// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Steeltoe.Extensions.Logging.DynamicSerilog.Test
{
    public class SerilogDynamicWebhostBuilderTest
    {
        [Fact]
        public void OnlyApplicableFilters_AreApplied()
        {
            // arrange
            var testSink = new TestSink();

            // act
            var host = new WebHostBuilder()
                .UseStartup<Startup>()
                .AddDynamicSerilog((context, loggerConfiguration) =>
                {
                    loggerConfiguration
                        .MinimumLevel.Error()
                        .Enrich.WithExceptionDetails()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                        .WriteTo.Sink(testSink, LogEventLevel.Error);
                })
                .Build();

            // assert
            var logs = testSink.GetLogs();
            Assert.NotEmpty(logs);
            Assert.Contains("error", logs);
            Assert.DoesNotContain("info", logs);
        }

        [Fact]
        public void AddDynamicSerilog_Default_AddsConsole()
        {
            // act
            var host = new WebHostBuilder()
                .UseStartup<Startup>()
                .AddDynamicSerilog()
                .Build();

            // assert
            var logger = (Logger)host.Services.GetService(typeof(Logger));
            var loggerSinksField = logger.GetType().GetField("_sink", BindingFlags.NonPublic | BindingFlags.Instance);
            var aggregatedSinks = loggerSinksField.GetValue(logger);
            var aggregateSinksField = aggregatedSinks.GetType().GetField("_sinks", BindingFlags.NonPublic | BindingFlags.Instance);
            var sinks = (ILogEventSink[])aggregateSinksField.GetValue(aggregatedSinks);
            Assert.Single(sinks);
            Assert.Equal("Serilog.Sinks.SystemConsole.ConsoleSink", sinks.First().GetType().FullName);
        }

        [Fact]
        public void AddDynamicSerilog_ReadsConfig_AddsConsole()
        {
            // arrange
            var appSettings = new Dictionary<string, string> { { "Serilog:WriteTo:0:Name", "Console" } };

            // act
            var host = new WebHostBuilder()
                .ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(appSettings))
                .UseStartup<Startup>()
                .AddDynamicSerilog()
                .Build();

            // assert
            var logger = (Logger)host.Services.GetService(typeof(Logger));
            var loggerSinksField = logger.GetType().GetField("_sink", BindingFlags.NonPublic | BindingFlags.Instance);
            var aggregatedSinks = loggerSinksField.GetValue(logger);
            var aggregateSinksField = aggregatedSinks.GetType().GetField("_sinks", BindingFlags.NonPublic | BindingFlags.Instance);
            var sinks = (ILogEventSink[])aggregateSinksField.GetValue(aggregatedSinks);

            // note: there could be two here without the logic in SerilogConfigurationExtensions.AddConsoleIfNoSinksFound
            Assert.Single(sinks);
            Assert.Equal("Serilog.Sinks.SystemConsole.ConsoleSink", sinks.First().GetType().FullName);
        }
    }
}
